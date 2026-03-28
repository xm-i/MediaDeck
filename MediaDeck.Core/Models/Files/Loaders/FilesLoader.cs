using MediaDeck.Composition.Interfaces.Files;
using MediaDeck.Composition.Interfaces.FileTypes.Models;
using MediaDeck.Core.Models.Files.Filter;
using MediaDeck.Core.Models.Files.SearchConditions;
using MediaDeck.Core.Models.Files.Sort;
using MediaDeck.Core.Utils;
using MediaDeck.Database;

namespace MediaDeck.Core.Models.Files.Loaders;

[Inject(InjectServiceLifetime.Transient)]
public class FilesLoader(IDbContextFactory<MediaDeckDbContext> dbFactory, SortSelector sortSelector,FilterSelector filterSetter) {
	protected FilterSelector FilterSetter = filterSetter;
	protected SortSelector SortSelector = sortSelector;

	public async Task<IEnumerable<IFileModel>> Load(IEnumerable<ISearchCondition> searchConditions, CancellationToken cancellationToken = default) {
		await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
		var files =
			(await db
				.MediaFiles
				.AsNoTracking()
				.Where(searchConditions)
				.Include(mf => mf.MediaFileTags)
				.ThenInclude(mft => mft.Tag)
				.ThenInclude(t => t.TagCategory)
				.Include(mf => mf.MediaFileTags)
				.ThenInclude(mft => mft.Tag)
				.ThenInclude(t => t.TagAliases)
				.Include(mf => mf.Position)
				.IncludeTables()
				.AsSplitQuery()
				.ToArrayAsync(cancellationToken))
				.Select(FileTypeUtility.CreateFileModelFromRecord)
				.Where(searchConditions)
				.Where(this.FilterSetter);

		return this.SortSelector.SetSortConditions(files);
	}
}

