using System.Collections.Generic;
using System.Threading.Tasks;

using MediaDeck.Composition.Interfaces.Files;
using MediaDeck.Composition.Interfaces.FileTypes.Models;
using MediaDeck.Database;
using MediaDeck.Models.Files.Filter;
using MediaDeck.Models.Files.SearchConditions;
using MediaDeck.Models.Files.Sort;
using MediaDeck.Utils.Constants;

namespace MediaDeck.Models.Files.Loaders;

[AddTransient]
public class FilesLoader(MediaDeckDbContext dbContext, SortSelector sortSelector,FilterSelector filterSetter) {
	protected FilterSelector FilterSetter = filterSetter;
	protected SortSelector SortSelector = sortSelector;

	public async Task<IEnumerable<IFileModel>> Load(IEnumerable<ISearchCondition> searchConditions) {
		using var lockObject = await LockObjectConstants.DbLock.LockAsync();
		var files =
			(await dbContext
				.MediaFiles
				.Where(searchConditions)
				.Include(mf => mf.MediaFileTags)
				.ThenInclude(mft => mft.Tag)
				.ThenInclude(t => t.TagCategory)
				.Include(mf => mf.MediaFileTags)
				.ThenInclude(mft => mft.Tag)
				.ThenInclude(t => t.TagAliases)
				.Include(mf => mf.Position)
				.IncludeTables()
				.ToArrayAsync())
				.Select(FileTypeUtility.CreateFileModelFromRecord)
				.Where(searchConditions)
				.Where(this.FilterSetter);

		return this.SortSelector.SetSortConditions(files);
	}
}

