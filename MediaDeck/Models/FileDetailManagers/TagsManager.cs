using System.Collections.Generic;
using System.Threading.Tasks;

using MediaDeck.Composition.Interfaces.Files;
using MediaDeck.Composition.Interfaces.FileTypes.Models;
using MediaDeck.Database;
using MediaDeck.Database.Tables;
using MediaDeck.Models.Files;
using MediaDeck.Utils.Constants;

namespace MediaDeck.Models.FileDetailManagers;

[Inject(InjectServiceLifetime.Singleton)]
public class TagsManager(MediaDeckDbContext dbContext) {
	private readonly MediaDeckDbContext _db = dbContext;

	public ObservableList<TagCategory> TagCategories {
		get;
	} = [];

	public ObservableList<ITagModel> Tags {
		get;
	} = [];

	public async Task AddTagAsync(IFileModel[] fileModels, string tagName, string detail = "") {
		var target = fileModels.Where(x => !x.Tags.Any(t => t.TagName == tagName)).ToArray();
		using var lockObject = await LockObjectConstants.DbLock.LockAsync();
		using var transaction = await this._db.Database.BeginTransactionAsync();
		var tag = await this._db.Tags.FirstOrDefaultAsync(x => x.TagName == tagName);
		if(tag == null) {
			var targetCategory = this.TagCategories.MinBy(x => x.TagCategoryId)!;
			tag = new Tag {
				TagCategoryId = targetCategory.TagCategoryId,
				TagName = tagName,
				Detail = detail,
				TagAliases = [],
				TagCategory = targetCategory
			};
			await this._db.AddAsync(tag);
			await this._db.SaveChangesAsync();
		}
		await this._db.MediaFileTags.AddRangeAsync(target.Select(x => new MediaFileTag {
			MediaFileId = x.Id,
			TagId = tag.TagId
		}));
		await this._db.SaveChangesAsync();
		foreach(var file in target) {
			file.Tags.Add(new TagModel(tag));
		}
		await transaction.CommitAsync();
	}

	public async Task RemoveTagAsync(IFileModel[] fileModels, int tagId) {
		var ids = fileModels.Select(x => x.Id);
		using var lockObject = await LockObjectConstants.DbLock.LockAsync();
		using var transaction = await this._db.Database.BeginTransactionAsync();
		var rel =
			await
			this._db
				.MediaFileTags
				.Where(x => ids.Contains(x.MediaFileId) && x.Tag.TagId == tagId)
				.ToArrayAsync();
		if (!rel.IsEmpty()) {
			this._db.MediaFileTags.RemoveRange(rel);
			await this._db.SaveChangesAsync();
			foreach (var file in fileModels) {
				file.Tags.RemoveAll(x => x.TagId== tagId);
			}
			await transaction.CommitAsync();
		}
	}

	public async Task UpdateTagAsync(int tagId, int tagCategoryId, string tagName, string detail, IEnumerable<TagAlias> aliases) {
		using var lockObject = await LockObjectConstants.DbLock.LockAsync();
		using var transaction = await this._db.Database.BeginTransactionAsync();
		var tag = this._db.Tags.First(x => x.TagId == tagId);
		tag.TagCategoryId = tagCategoryId;
		tag.TagName = tagName;
		tag.Detail = detail;
		this._db.Tags.Update(tag);

		this._db.TagAliases.RemoveRange(this._db.TagAliases.Where(x => x.TagId == tagId));
		await this._db.TagAliases.AddRangeAsync(aliases.Select((x,i) => new TagAlias {
			TagId = tagId,
			TagAliasId = i,
			Alias = x.Alias,
			Ruby = x.Ruby
		}));

		await this._db.SaveChangesAsync();
		await transaction.CommitAsync();
	}

	public async Task UpdateTagCategoryAsync(int tagCategoryId, string tagCategoryName, string detail) {
		using var lockObject = await LockObjectConstants.DbLock.LockAsync();
		using var transaction = await this._db.Database.BeginTransactionAsync();
		var tagCategory = await this._db.TagCategories.FirstOrDefaultAsync(x => x.TagCategoryId == tagCategoryId);
		if (tagCategory != null) {
			tagCategory.TagCategoryName = tagCategoryName;
			tagCategory.Detail = detail;
			this._db.TagCategories.Update(tagCategory);
		} else {
			tagCategory = new TagCategory() {
				TagCategoryId = tagCategoryId,
				TagCategoryName = tagCategoryName,
				Detail = detail,
				Tags = []
			};
			await this._db.TagCategories.AddAsync(tagCategory);
		}
		await this._db.SaveChangesAsync();
		await transaction.CommitAsync();
	}

	public async Task Load() {
		using var lockObject = await LockObjectConstants.DbLock.LockAsync();
		this.TagCategories.Clear();
		this.Tags.Clear();
		var tagCategories =
			await
				this._db.TagCategories
					.Include(x => x.Tags)
					.ThenInclude(x => x.TagAliases)
					.Include(x => x.Tags)
					.ThenInclude(x => x.MediaFileTags)
					.ToArrayAsync();
		foreach (var tag in tagCategories.SelectMany(x => x.Tags).OrderByDescending(x => x.MediaFileTags.Count)) {
			var newTag = new TagModel(tag);
			this.Tags.Add(newTag);
		}
		this.TagCategories.AddRange(tagCategories);
	}
}
