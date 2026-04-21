using System.Runtime.CompilerServices;
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
	/// 検索条件に基づき、IAsyncEnumerable でストリーミング形式でファイルを取得します。
	/// 列挙中のみ DbContext が維持されます。
	/// </summary>
	/// <param name="searchConditions">検索条件</param>
	/// <param name="cancellationToken">キャンセルトークン</param>
	/// <returns>IFileModelのストリーム</returns>
	public async IAsyncEnumerable<IFileModel> GetFilesStreamAsync(IEnumerable<ISearchCondition> searchConditions, [EnumeratorCancellation] CancellationToken cancellationToken = default) {
		await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
		var query = this.BuildQuery(db, searchConditions);

		await foreach (var item in query.AsAsyncEnumerable().WithCancellation(cancellationToken)) {
			yield return this._fileTypeService.CreateFileModelFromRecord(item);
		}
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