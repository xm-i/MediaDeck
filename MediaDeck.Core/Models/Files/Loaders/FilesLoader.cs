using System.Runtime.CompilerServices;
using MediaDeck.Composition.Database;
using MediaDeck.Composition.Interfaces.Files;
using MediaDeck.Composition.Interfaces.MediaItemTypes;
using MediaDeck.Composition.Interfaces.MediaItemTypes.Models;
using MediaDeck.Composition.Tables;
using MediaDeck.Core.Models.Files.Filter;
using MediaDeck.Core.Models.Files.SearchConditions;
using MediaDeck.Core.Models.Files.Sort;


namespace MediaDeck.Core.Models.Files.Loaders;

[Inject(InjectServiceLifetime.Scoped)]
public class FilesLoader(IDbContextFactory<MediaDeckDbContext> dbFactory, SortSelector sortSelector, FilterSelector filterSetter, IMediaItemTypeService MediaItemTypeService) {
	protected FilterSelector FilterSetter = filterSetter;
	protected SortSelector SortSelector = sortSelector;
	private readonly IMediaItemTypeService _MediaItemTypeService = MediaItemTypeService;

	/// <summary>
	/// 検索条件に基づき、IAsyncEnumerable でストリーミング形式でファイルを取得します。
	/// 列挙中のみ DbContext が維持されます。
	/// </summary>
	/// <param name="searchConditions">検索条件</param>
	/// <param name="skip">スキップ件数</param>
	/// <param name="take">取得件数</param>
	/// <param name="cancellationToken">キャンセルトークン</param>
	/// <returns>IMediaItemModelのストリーム</returns>
	public virtual async IAsyncEnumerable<IMediaItemModel> GetFilesStreamAsync(IEnumerable<ISearchCondition> searchConditions, int? skip = null, int? take = null, [EnumeratorCancellation] CancellationToken cancellationToken = default) {
		await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
		var query = this.BuildQuery(db, searchConditions);

		if (skip.HasValue) {
			query = query.Skip(skip.Value);
		}
		if (take.HasValue) {
			query = query.Take(take.Value);
		}

		await foreach (var item in query.AsAsyncEnumerable().WithCancellation(cancellationToken)) {
			yield return this._MediaItemTypeService.CreateMediaItemModelFromRecord(item);
		}
	}

	/// <summary>
	/// 検索条件に基づき、全件数を取得します。
	/// </summary>
	/// <param name="searchConditions">検索条件</param>
	/// <param name="cancellationToken">キャンセルトークン</param>
	/// <returns>全件数</returns>
	public virtual async Task<int> GetTotalCountAsync(IEnumerable<ISearchCondition> searchConditions, CancellationToken cancellationToken = default) {
		await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
		var query = db.MediaItems.AsNoTracking().Where(searchConditions).Where(this.FilterSetter);
		return await query.CountAsync(cancellationToken);
	}

	/// <summary>
	/// 検索・フィルター・ソートを適用した IQueryable パイプラインを構築する
	/// </summary>
	private IQueryable<MediaItem> BuildQuery(MediaDeckDbContext db, IEnumerable<ISearchCondition> searchConditions) {
		IQueryable<MediaItem> query = db
			.MediaItems
			.AsNoTracking()
			.Where(searchConditions)
			.Where(this.FilterSetter);

		query = this.SortSelector.SetSortConditions(query);

		query = query
			.Include(mf => mf.MediaItemTags)
			.ThenInclude(mft => mft.Tag)
			.ThenInclude(t => t.TagCategory)
			.Include(mf => mf.MediaItemTags)
			.ThenInclude(mft => mft.Tag)
			.ThenInclude(t => t.TagAliases)
			.Include(mf => mf.Position);

		query = this._MediaItemTypeService.IncludeTables(query);

		return query;
	}
}