#nullable enable
using System.Collections.Generic;
using System.Threading.Tasks;

using MediaDeck.Composition.Interfaces.Files;
using MediaDeck.Composition.Interfaces.FileTypes.Models;
using MediaDeck.Database;
using MediaDeck.Database.Tables;
using MediaDeck.Models.Files;

namespace MediaDeck.Models.FileDetailManagers;

[Inject(InjectServiceLifetime.Singleton)]
public class TagsManager(IDbContextFactory<MediaDeckDbContext> dbFactory) {
	private readonly IDbContextFactory<MediaDeckDbContext> _dbFactory = dbFactory;

	public ObservableList<TagCategory> TagCategories {
		get;
	} = [];

	public ObservableList<ITagModel> Tags {
		get;
	} = [];

	public async Task<Tag?> FindTagByNameAsync(string tagName) {
		await using var db = await this._dbFactory.CreateDbContextAsync();
		var tag = await db.Tags.FirstOrDefaultAsync(x => x.TagName == tagName);
		return tag;
	}

	public async Task<Tag> CreateTagAsync(int tagCategoryId, string tagName, string detail, IEnumerable<TagAlias> aliases) {
		await using var db = await this._dbFactory.CreateDbContextAsync();
		using var transaction = await db.Database.BeginTransactionAsync();
		var tag = new Tag {
			TagCategoryId = tagCategoryId,
			TagName = tagName,
			Detail = detail,
			TagAliases = [],
			TagCategory = null!
		};
		await db.AddAsync(tag);
		await db.SaveChangesAsync();

		var aliasList = aliases.Select((x, i) => new TagAlias {
			TagId = tag.TagId,
			TagAliasId = i,
			Alias = x.Alias,
			Ruby = x.Ruby
		}).ToArray();
		
		if (aliasList.Length > 0) {
			await db.TagAliases.AddRangeAsync(aliasList);
			await db.SaveChangesAsync();
		}
		
		await transaction.CommitAsync();
		return tag;
	}

	public async Task AddTagAsync(IFileModel[] fileModels, Tag tag) {
		var target = fileModels.Where(x => !x.Tags.Any(t => t.TagId == tag.TagId)).ToArray();
		if (target.IsEmpty()) {
			return;
		}
		
		await using var db = await this._dbFactory.CreateDbContextAsync();
		using var transaction = await db.Database.BeginTransactionAsync();
		if (tag == null) {
			await transaction.RollbackAsync();
			return;
		}
		
		await db.MediaFileTags.AddRangeAsync(target.Select(x => new MediaFileTag {
			MediaFileId = x.Id,
			TagId = tag.TagId
		}));
		await db.SaveChangesAsync();
		foreach(var file in target) {
			file.Tags.Add(new TagModel(tag));
		}
		await transaction.CommitAsync();
	}

	public async Task RemoveTagAsync(IFileModel[] fileModels, int tagId) {
		var ids = fileModels.Select(x => x.Id);
		await using var db = await this._dbFactory.CreateDbContextAsync();
		using var transaction = await db.Database.BeginTransactionAsync();
		var rel =
			await
			db
				.MediaFileTags
				.Where(x => ids.Contains(x.MediaFileId) && x.Tag.TagId == tagId)
				.ToArrayAsync();
		if (!rel.IsEmpty()) {
			db.MediaFileTags.RemoveRange(rel);
			await db.SaveChangesAsync();
			foreach (var file in fileModels) {
				file.Tags.RemoveAll(x => x.TagId== tagId);
			}
			await transaction.CommitAsync();
		}
	}

	public async Task UpdateTagAsync(int tagId, int tagCategoryId, string tagName, string detail, IEnumerable<TagAlias> aliases) {
		await using var db = await this._dbFactory.CreateDbContextAsync();
		using var transaction = await db.Database.BeginTransactionAsync();
		var tag = await db.Tags.FirstAsync(x => x.TagId == tagId);
		tag.TagCategoryId = tagCategoryId;
		tag.TagName = tagName;
		tag.Detail = detail;
		db.Tags.Update(tag);

		db.TagAliases.RemoveRange(db.TagAliases.Where(x => x.TagId == tagId));
		await db.TagAliases.AddRangeAsync(aliases.Select((x,i) => new TagAlias {
			TagId = tagId,
			TagAliasId = i,
			Alias = x.Alias,
			Ruby = x.Ruby
		}));

		await db.SaveChangesAsync();
		await transaction.CommitAsync();
	}

	public async Task UpdateTagCategoryAsync(int tagCategoryId, string tagCategoryName, string detail) {
		await using var db = await this._dbFactory.CreateDbContextAsync();
		using var transaction = await db.Database.BeginTransactionAsync();
		var tagCategory = await db.TagCategories.FirstOrDefaultAsync(x => x.TagCategoryId == tagCategoryId);
		if (tagCategory != null) {
			tagCategory.TagCategoryName = tagCategoryName;
			tagCategory.Detail = detail;
			db.TagCategories.Update(tagCategory);
		} else {
			tagCategory = new TagCategory() {
				TagCategoryId = tagCategoryId,
				TagCategoryName = tagCategoryName,
				Detail = detail,
				Tags = []
			};
			await db.TagCategories.AddAsync(tagCategory);
		}
		await db.SaveChangesAsync();
		await transaction.CommitAsync();
	}

	public async Task Load() {
		await using var db = await this._dbFactory.CreateDbContextAsync();
		this.TagCategories.Clear();
		this.Tags.Clear();
		var tagCategories =
			await
				db.TagCategories
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
