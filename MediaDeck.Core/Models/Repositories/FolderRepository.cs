using MediaDeck.Common.Extensions;
using MediaDeck.Composition.Stores.State.Model;
using MediaDeck.Core.Models.Files.SearchConditions;
using MediaDeck.Core.Models.NotificationDispatcher;
using MediaDeck.Core.Models.Repositories.Objects;
using MediaDeck.Core.Primitives;
using MediaDeck.Database;

namespace MediaDeck.Core.Models.Repositories;

[Inject(InjectServiceLifetime.Scoped)]
public class FolderRepository : RepositoryBase {
	private readonly TabStateModel _tabState;

	public FolderRepository(IDbContextFactory<MediaDeckDbContext> dbFactory, SearchConditionNotificationDispatcher searchConditionNotificationDispatcher, TabStateModel tabState) {
		this._dbFactory = dbFactory;
		this._searchConditionNotificationDispatcher = searchConditionNotificationDispatcher;
		this._tabState = tabState;
		FileNotifications
			.FileRegistered
			.ThrottleLast(TimeSpan.FromSeconds(10))
			.Subscribe(async _ => await this.Load())
			.AddTo(this.CompositeDisposable);
	}

	private readonly IDbContextFactory<MediaDeckDbContext> _dbFactory;
	private readonly SearchConditionNotificationDispatcher _searchConditionNotificationDispatcher;

	public ReactiveProperty<FolderObject> RootFolder {
		get;
	} = new();

	public ValueCountPair<string>[] _currentDirectoryStatusList = [];

	public override async Task Load() {
		await using var db = await this._dbFactory.CreateDbContextAsync();
		var list = (await db
				.MediaItems
				.GroupBy(x => x.DirectoryPath)
				.Select(x => new ValueCountPair<string>(x.Key, x.Count()))
				.ToListAsync())
			.OrderBy(x => x.Value)
			.ToList();

		var directoryStatusList = list.ToArray();
		if (directoryStatusList.SequenceEqual(this._currentDirectoryStatusList)) {
			this.RootFolder.Value ??= new FolderObject(null, "", []);
			return;
		}
		this._currentDirectoryStatusList = directoryStatusList;

		var all = list.Select(x => (x.Value, x.Count, Split: x.Value.Split(Path.DirectorySeparatorChar))).ToArray();
		var maxPathDepth = all.Length == 0 ? 0 : all.Max(x => x.Split.Length);

		// ルート追加
		list.Add(new("", list.Sum(x => x.Count)));

		// 足りない部分を補足
		for (var depth = 1; depth <= maxPathDepth; depth++) {
			// 最大の深さまで
			var target = all.Where(x => x.Split.Length > depth).ToArray(); // 深さが足りないものを除外
			list.AddRange(target
				.Select(x =>
				(
					item: x,
					joined: string.Join(Path.DirectorySeparatorChar, x.Split[0..depth]), // 対象の深さまでのパス
					joinedPlus: string.Join(Path.DirectorySeparatorChar, x.Split[0..(depth + 1)]) // 対象の深さ+1までのパス
				))
				.Where(x => !all.Any(y => y.Value == x.joined)) // 対象のパスがすでに含まれていたら除外
				.GroupBy(x => x.joined) // 対象の深さまでのパスでグループ化
				.Where(x => x.DistinctBy(x => x.joinedPlus).Count() >= 2) // 対象の深さから、2つ以上のフォルダに分岐している
				.Select(x => new ValueCountPair<string>(x.Key, 0)));
		}

		this.RootFolder.Value = new FolderObject(null, "", [.. list.OrderBy(x => x.Value)]);

		this.Restore();
	}

	public void SetRepositoryCandidate(FolderObject folderObject, bool includeSubDirectory) {
		this._searchConditionNotificationDispatcher.UpdateRequest.OnNext(x => {
			x.RemoveRange(x.Where(x => x is FolderSearchCondition));
			x.Add(new FolderSearchCondition { FolderPath = folderObject.FolderPath, IncludeSubDirectories = includeSubDirectory });
		});
	}

	public IEnumerable<FolderObject> GetAllFolders() {
		this.Load().Wait();
		IEnumerable<FolderObject> func(FolderObject fo) => fo.ChildFolders.SelectMany(x => func(x)).Concat([fo]).OrderBy(x => x.FolderPath);
		return func(this.RootFolder.Value);
	}

	private void Restore() {
		var condition = this._tabState.SearchState.SearchCondition.FirstOrDefault(x => x is FolderSearchCondition) as FolderSearchCondition;
		if (condition == null) {
			return;
		}
		var parent = this.RootFolder.Value;
		while (true) {
			var previousParent = parent;
			foreach (var child in parent.ChildFolders) {
				if (condition.FolderPath.StartsWith(child.FolderPath)) {
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