using System.Runtime.CompilerServices;
using MediaDeck.Common.Base;
using MediaDeck.Composition.Database;
using MediaDeck.Composition.Interfaces.Files;
using MediaDeck.Composition.Interfaces.MediaItemTypes;
using MediaDeck.Composition.Interfaces.MediaItemTypes.Models;
using MediaDeck.Composition.Tables;
using MediaDeck.Core.Models.Files.Filter;
using MediaDeck.Core.Models.Files.SearchConditions;
using MediaDeck.Core.Models.Files.Sort;

namespace MediaDeck.Core.Models.Files.Loaders;

/// <summary>
/// 検索・フィルター・ソート条件に基づき、メディアファイルを読み込むローダークラス
/// </summary>
[Inject(InjectServiceLifetime.Scoped)]
public class FilesLoader : DisposableBase {
	private readonly IDbContextFactory<MediaDeckDbContext> _dbFactory;
	private readonly IMediaItemTypeService _mediaItemTypeService;
	protected FilterSelector? FilterSetter {
		get;
	}
	protected SortSelector? SortSelector {
		get;
	}

	/// <summary>
	/// <see cref="FilesLoader"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	public FilesLoader(
		IDbContextFactory<MediaDeckDbContext> dbFactory,
		SortSelector? sortSelector,
		FilterSelector? filterSetter,
		IMediaItemTypeService mediaItemTypeService) {
		this._dbFactory = dbFactory;
		this.SortSelector = sortSelector;
		this.FilterSetter = filterSetter;
		this._mediaItemTypeService = mediaItemTypeService;
	}

	/// <summary>
	/// 検索条件に基づき、IAsyncEnumerable でストリーミング形式でファイルを取得します。
	/// 列挙中のみ DbContext が維持されます。
	/// </summary>
	/// <param name="searchConditions">検索条件</param>
	/// <param name="skip">スキップ件数</param>
	/// <param name="take">取得件数</param>
	/// <param name="cancellationToken">キャンセルトークン</param>
	/// <returns>IMediaItemModelのストリーム</returns>
	public virtual async IAsyncEnumerable<IMediaItemModel> GetFilesStreamAsync(
		IEnumerable<ISearchCondition> searchConditions,
		int? skip = null,
		int? take = null,
		[EnumeratorCancellation] CancellationToken cancellationToken = default) {
		await using var db = await this._dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
		var query = this.BuildQuery(db, searchConditions);

		if (skip.HasValue) {
			query = query.Skip(skip.Value);
		}
		if (take.HasValue) {
			query = query.Take(take.Value);
		}

		await foreach (var item in query.AsAsyncEnumerable().WithCancellation(cancellationToken).ConfigureAwait(false)) {
			yield return this._mediaItemTypeService.CreateMediaItemModelFromRecord(item);
		}
	}

	/// <summary>
	/// 検索条件に基づき、全件数を取得します。
	/// </summary>
	/// <param name="searchConditions">検索条件</param>
	/// <param name="cancellationToken">キャンセルトークン</param>
	/// <returns>全件数</returns>
	public virtual async Task<int> GetTotalCountAsync(IEnumerable<ISearchCondition> searchConditions, CancellationToken cancellationToken = default) {
		await using var db = await this._dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
		var query = db.MediaItems.AsNoTracking().Where(searchConditions);
		if (this.FilterSetter != null) {
			query = query.Where(this.FilterSetter);
		}
		return await query.CountAsync(cancellationToken).ConfigureAwait(false);
	}

	/// <summary>
	/// テーブル結合（Include）を適用したクエリを取得します。
	/// </summary>
	private IQueryable<MediaItem> ApplyIncludes(IQueryable<MediaItem> query) {
		query = query
			.AsSplitQuery()
			.Include(mf => mf.MediaItemTags)
			.ThenInclude(mft => mft.Tag)
			.ThenInclude(t => t.TagCategory)
			.Include(mf => mf.MediaItemTags)
			.ThenInclude(mft => mft.Tag)
			.ThenInclude(t => t.TagAliases)
			.Include(mf => mf.Position);

		return this._mediaItemTypeService.IncludeTables(query);
	}

	/// <summary>
	/// 検索・フィルター・ソートを適用した IQueryable パイプラインを構築する
	/// </summary>
	private IQueryable<MediaItem> BuildQuery(MediaDeckDbContext db, IEnumerable<ISearchCondition> searchConditions) {
		IQueryable<MediaItem> query = db
			.MediaItems
			.AsNoTracking()
			.Where(searchConditions);

		if (this.FilterSetter != null) {
			query = query.Where(this.FilterSetter);
		}

		if (this.SortSelector != null) {
			query = this.SortSelector.SetSortConditions(query);
		}
		query = this.ApplyIncludes(query);

		return query;
	}
}