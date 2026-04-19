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

	public async Task<IEnumerable<IFileModel>> Load(IEnumerable<ISearchCondition> searchConditions, CancellationToken cancellationToken = default) {
		await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
		IQueryable<MediaFile> query = db
				.MediaFiles
				.AsNoTracking()
				.Where(searchConditions)
				.Include(mf => mf.MediaFileTags)
				.ThenInclude(mft => mft.Tag)
				.ThenInclude(t => t.TagCategory)
				.Include(mf => mf.MediaFileTags)
				.ThenInclude(mft => mft.Tag)
				.ThenInclude(t => t.TagAliases)
				.Include(mf => mf.Position);


		query = this._fileTypeService.IncludeTables(query);

		var files =
			(await query
				.AsSplitQuery()
				.ToArrayAsync(cancellationToken))
			.Select(this._fileTypeService.CreateFileModelFromRecord)
			.Where(searchConditions)
			.Where(this.FilterSetter);

		return this.SortSelector.SetSortConditions(files);
	}
}