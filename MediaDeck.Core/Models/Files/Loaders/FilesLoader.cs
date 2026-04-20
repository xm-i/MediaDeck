using MediaDeck.Composition.Interfaces.Files;
using MediaDeck.Composition.Interfaces.FileTypes;
using MediaDeck.Composition.Interfaces.FileTypes.Models;
using MediaDeck.Core.Models.Files.Filter;
using MediaDeck.Core.Models.Files.SearchConditions;
using MediaDeck.Core.Models.Files.Sort;
using MediaDeck.Database;
using MediaDeck.Database.Tables;


namespace MediaDeck.Core.Models.Files.Loaders;

[Inject(InjectServiceLifetime.Scoped)]
public class FilesLoader(IDbContextFactory<MediaDeckDbContext> dbFactory, SortSelector sortSelector, FilterSelector filterSetter, IFileTypeService fileTypeService) {
	protected FilterSelector FilterSetter = filterSetter;
	protected SortSelector SortSelector = sortSelector;
	private readonly IFileTypeService _fileTypeService = fileTypeService;

	/// <summary>
	/// 全件取得（従来互換）
	/// </summary>
	public async Task<IEnumerable<IFileModel>> Load(IEnumerable<ISearchCondition> searchConditions, CancellationToken cancellationToken = default) {
		await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
		var query = this.BuildQuery(db, searchConditions);

		var files =
			(await query
				.AsSplitQuery()
				.ToArrayAsync(cancellationToken))
			.Select(this._fileTypeService.CreateFileModelFromRecord);

		return files;
	}

	/// <summary>
	/// ページネーション対応取得
	/// </summary>
	/// <param name="searchConditions">検索条件</param>
	/// <param name="skip">スキップ件数</param>
	/// <param name="take">取得件数</param>
	/// <param name="cancellationToken">キャンセルトークン</param>
	/// <returns>ページネーション結果（アイテムと総件数）</returns>
	public async Task<(IEnumerable<IFileModel> Items, int TotalCount)> LoadPaged(IEnumerable<ISearchCondition> searchConditions, int skip, int take, CancellationToken cancellationToken = default) {
		await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
		var query = this.BuildQuery(db, searchConditions);

		var totalCount = await query.CountAsync(cancellationToken);

		var files =
			(await query
				.Skip(skip)
				.Take(take)
				.AsSplitQuery()
				.ToArrayAsync(cancellationToken))
			.Select(this._fileTypeService.CreateFileModelFromRecord);

		return (files, totalCount);
	}

	/// <summary>
	/// 検索・フィルター・ソートを適用した IQueryable パイプラインを構築する
	/// </summary>
	private IQueryable<MediaFile> BuildQuery(MediaDeckDbContext db, IEnumerable<ISearchCondition> searchConditions) {
		IQueryable<MediaFile> query = db
			.MediaFiles
			.AsNoTracking()
			.Where(searchConditions)
			.Where(this.FilterSetter);

		query = this.SortSelector.SetSortConditions(query);

		query = query
			.Include(mf => mf.MediaFileTags)
			.ThenInclude(mft => mft.Tag)
			.ThenInclude(t => t.TagCategory)
			.Include(mf => mf.MediaFileTags)
			.ThenInclude(mft => mft.Tag)
			.ThenInclude(t => t.TagAliases)
			.Include(mf => mf.Position);

		query = this._fileTypeService.IncludeTables(query);

		return query;
	}
}