using MediaDeck.Common.Extensions;
using MediaDeck.Composition.Database;
using MediaDeck.Composition.Interfaces.Notifications;
using MediaDeck.Composition.Stores.State.Model;
using MediaDeck.Composition.Tables;
using MediaDeck.Core.Models.Files.SearchConditions;
using MediaDeck.Core.Models.NotificationDispatcher;
using MediaDeck.Core.Models.Repositories.Objects;
using MediaDeck.Core.Primitives;

namespace MediaDeck.Core.Models.Repositories;

/// <summary>
/// アルバムの階層構造および検索候補への反映を管理するリポジトリ。
/// </summary>
[Inject(InjectServiceLifetime.Scoped)]
public class AlbumRepository : RepositoryBase {
	private readonly IDbContextFactory<MediaDeckDbContext> _dbFactory;
	private readonly ISearchConditionNotificationDispatcher _searchConditionNotificationDispatcher;
	private readonly TabStateModel _tabState;

	/// <summary>
	/// <see cref="AlbumRepository"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	/// <param name="dbFactory">データベースコンテキストファクトリ。</param>
	/// <param name="searchConditionNotificationDispatcher">検索条件通知ディスパッチャ。</param>
	/// <param name="tabState">タブ状態モデル。</param>
	public AlbumRepository(IDbContextFactory<MediaDeckDbContext> dbFactory,
		ISearchConditionNotificationDispatcher searchConditionNotificationDispatcher,
		TabStateModel tabState) {
		this._dbFactory = dbFactory;
		this._searchConditionNotificationDispatcher = searchConditionNotificationDispatcher;
		this._tabState = tabState;
		FileNotifications
			.FileRegistered
			.ThrottleLast(TimeSpan.FromSeconds(10))
			.SubscribeAwait(async (_, ct) => await this.Load(), AwaitOperation.Sequential)
			.AddTo(this.CompositeDisposable);
		AlbumNotifications
			.AlbumUpdated
			.Where(sender => !ReferenceEquals(sender, this))
			.SubscribeAwait(async (_, ct) => await this.Load(), AwaitOperation.Sequential)
			.AddTo(this.CompositeDisposable);
	}

	public ReactiveProperty<AlbumObject> RootAlbum {
		get;
	} = new();

	private ValueCountPair<string>[] _currentAlbumStatusList = [];

	public override async Task Load() {
		await using var db = await this._dbFactory.CreateDbContextAsync();

		// 全アルバムと、それぞれの直接登録メディア件数
		var counts = await db.Albums
			.Select(a => new ValueCountPair<string>(a.Path, a.MediaItemAlbums.Count()))
			.ToListAsync();

		var list = counts
			.OrderBy(x => x.Value)
			.ToList();

		var statusArray = list.ToArray();
		if (statusArray.SequenceEqual(this._currentAlbumStatusList)) {
			this.RootAlbum.Value ??= new AlbumObject(null, "", []);
			return;
		}
		this._currentAlbumStatusList = statusArray;

		var all = list.Select(x => (x.Value, x.Count, Split: x.Value.Split(AlbumObject.Separator))).ToArray();
		var maxPathDepth = all.Length == 0 ? 0 : all.Max(x => x.Split.Length);

		// ルート
		list.Add(new("", list.Sum(x => x.Count)));

		// 中間ノード補完
		for (var depth = 1; depth <= maxPathDepth; depth++) {
			var target = all.Where(x => x.Split.Length > depth).ToArray();
			list.AddRange(target
				.Select(x => (
					item: x,
					joined: string.Join(AlbumObject.Separator, x.Split[0..depth]),
					joinedPlus: string.Join(AlbumObject.Separator, x.Split[0..(depth + 1)])))
				.Where(x => !all.Any(y => y.Value == x.joined))
				.GroupBy(x => x.joined)
				.Where(x => x.DistinctBy(x => x.joinedPlus).Count() >= 2)
				.Select(x => new ValueCountPair<string>(x.Key, 0)));
		}

		this.RootAlbum.Value = new AlbumObject(null, "", [.. list.OrderBy(x => x.Value)]);

		this.Restore();
	}

	public void SetRepositoryCandidate(AlbumObject albumObject, bool includeSubAlbums) {
		this._searchConditionNotificationDispatcher.UpdateRequest.OnNext(x => {
			x.RemoveRange(x.Where(c => c is IRepositorySearchCondition));
			x.Add(new AlbumSearchCondition { AlbumPath = albumObject.AlbumPath, IncludeSubAlbums = includeSubAlbums });
		});
	}

	/// <summary>
	/// 指定 Path のアルバムを取得。存在しない場合は新規作成。
	/// </summary>
	/// <param name="path">アルバムパス。</param>
	/// <returns>取得または作成された <see cref="Album"/>。</returns>
	public async Task<Album> GetOrCreateAsync(string path) {
		path = NormalizePath(path);
		await using var db = await this._dbFactory.CreateDbContextAsync();
		var existing = await db.Albums.FirstOrDefaultAsync(a => a.Path == path);
		if (existing != null) {
			return existing;
		}
		var album = new Album { Path = path };
		db.Albums.Add(album);
		await db.SaveChangesAsync();
		await this.Load();
		AlbumNotifications.AlbumUpdated.OnNext(this);
		return album;
	}

	/// <summary>
	/// 指定 Path の単アルバムを削除します。配下にサブアルバムが存在する場合は削除しません。
	/// </summary>
	/// <param name="path">削除するアルバムのパス。</param>
	/// <returns>削除できた場合は <c>true</c>、対象が存在しないか子アルバムがあるため削除できなかった場合は <c>false</c>。</returns>
	public async Task<bool> DeleteAlbumAsync(string path) {
		path = NormalizePath(path);
		var prefix = $"{path}{AlbumObject.Separator}";
		await using var db = await this._dbFactory.CreateDbContextAsync();

		if (await db.Albums.AnyAsync(a => a.Path.StartsWith(prefix))) {
			await this.Load();
			return false;
		}

		var album = await db.Albums
			.Include(a => a.MediaItemAlbums)
			.FirstOrDefaultAsync(a => a.Path == path);
		if (album == null) {
			await this.Load();
			return false;
		}

		db.Albums.Remove(album);
		await db.SaveChangesAsync();
		await this.Load();
		AlbumNotifications.AlbumUpdated.OnNext(this);
		return true;
	}

	/// <summary>
	/// 指定したメディアアイテム群を、指定 Path のアルバムへ追加する。
	/// アルバムが存在しなければ新規作成する。
	/// </summary>
	/// <param name="albumPath">アルバムパス。</param>
	/// <param name="mediaItemIds">追加するメディアアイテム ID 一覧。</param>
	public async Task AddItemsAsync(string albumPath, IEnumerable<long> mediaItemIds) {
		var path = NormalizePath(albumPath);
		await using var db = await this._dbFactory.CreateDbContextAsync();
		var album = await db.Albums.FirstOrDefaultAsync(a => a.Path == path);
		if (album == null) {
			album = new Album { Path = path };
			db.Albums.Add(album);
			await db.SaveChangesAsync();
		}
		album.LastAccessedTime = DateTime.UtcNow;

		var ids = mediaItemIds.Distinct().ToArray();
		var existingPairs = await db.MediaItemAlbums
			.Where(mia => mia.AlbumId == album.AlbumId && ids.Contains(mia.MediaItemId))
			.Select(mia => mia.MediaItemId)
			.ToListAsync();

		foreach (var id in ids.Except(existingPairs)) {
			db.MediaItemAlbums.Add(new MediaItemAlbum {
				AlbumId = album.AlbumId,
				MediaItemId = id,
			});
		}
		await db.SaveChangesAsync();
		await this.Load();
		AlbumNotifications.AlbumUpdated.OnNext(this);
	}

	/// <summary>
	/// 全アルバムを最近使用順で取得。
	/// </summary>
	public async Task<IReadOnlyList<Album>> GetRecentAlbumsAsync(int max = 20) {
		await using var db = await this._dbFactory.CreateDbContextAsync();
		return await db.Albums
			.AsNoTracking()
			.OrderByDescending(a => a.LastAccessedTime)
			.Take(max)
			.ToListAsync();
	}

	private static string NormalizePath(string path) {
		path = (path ?? "").Trim().Replace('\\', AlbumObject.Separator);
		path = path.Trim(AlbumObject.Separator);
		if (string.IsNullOrEmpty(path)) {
			throw new ArgumentException("Album path must not be empty.");
		}
		return path;
	}

	private void Restore() {
		var condition = this._tabState.SearchState.SearchCondition.OfType<AlbumSearchCondition>().FirstOrDefault();
		if (condition == null) {
			return;
		}
		var parent = this.RootAlbum.Value;
		while (true) {
			var previousParent = parent;
			foreach (var child in parent.ChildAlbums) {
				if (condition.AlbumPath.StartsWith(child.AlbumPath)) {
					child.IsExpanded = true;
					parent = child;
					break;
				}
			}
			if (previousParent == parent) {
				break;
			}
		}
	}
}