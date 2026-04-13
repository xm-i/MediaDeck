using MediaDeck.Common.Utilities;
using MediaDeck.Composition.Interfaces.FileTypes.Models;
using MediaDeck.Composition.Interfaces.Tags;
using MediaDeck.Database;
using MediaDeck.Database.Tables;

namespace MediaDeck.Core.Models.Tags;

[Inject(InjectServiceLifetime.Singleton, typeof(ITagsManager))]
public class TagsManager(IDbContextFactory<MediaDeckDbContext> dbFactory, ITagModelFactory tagModelFactory) : ITagsManager {
	private readonly IDbContextFactory<MediaDeckDbContext> _dbFactory = dbFactory;
	private readonly ITagModelFactory _tagModelFactory = tagModelFactory;
	private bool _isInitialized;

	public ObservableList<ITagCategoryModel> TagCategories {
		get;
	} = [];

	public ObservableList<ITagModel> Tags {
		get;
	} = [];

	public async Task<ITagModel?> FindTagByNameAsync(string tagName) {
		var tag = this.Tags.FirstOrDefault(x => x.TagName == tagName);
		return tag;
	}

	public async Task<ITagModel?> CreateTagAsync(int? tagCategoryId, string tagName, string detail, IEnumerable<ITagAliasModel> aliases) {
		await using var db = await this._dbFactory.CreateDbContextAsync();
		using var transaction = await db.Database.BeginTransactionAsync();
		var tag = new Tag {
			TagCategoryId = tagCategoryId,
			TagName = tagName,
			Detail = detail,
			TagAliases = [],
			TagCategory = null
		};
		await db.AddAsync(tag);
		await db.SaveChangesAsync();
		await db.Entry(tag).Reference(x => x.TagCategory).LoadAsync();

		var aliasList = aliases.Select((x, i) => new TagAlias { TagId = tag.TagId, TagAliasId = i, Alias = x.Alias, Ruby = x.Ruby }).ToArray();

		if (aliasList.Length > 0) {
			await db.TagAliases.AddRangeAsync(aliasList);
			await db.SaveChangesAsync();
		}

		await transaction.CommitAsync();

		var categoryModel = this.TagCategories.FirstOrDefault(x => x.TagCategoryId == tagCategoryId) ?? this.TagCategories.First(x => x.TagCategoryId == null);
		var newTagModel = this._tagModelFactory.Create(tag, categoryModel);
		categoryModel.Tags.Add(newTagModel);
		this.Tags.Add(newTagModel);
		return newTagModel;
	}

	public async Task AddTagAsync(IFileModel[] fileModels, ITagModel tag) {
		var target = fileModels.Where(x => !x.Tags.Any(t => t.TagId == tag.TagId)).ToArray();
		if (!target.Any()) {
			return;
		}

		await using var db = await this._dbFactory.CreateDbContextAsync();
		using var transaction = await db.Database.BeginTransactionAsync();
		if (tag == null) {
			await transaction.RollbackAsync();
			return;
		}

		await db.MediaFileTags.AddRangeAsync(target.Select(x => new MediaFileTag { MediaFileId = x.Id, TagId = tag.TagId }));
		await db.SaveChangesAsync();
		foreach (var file in target) {
			file.Tags.Add(tag);
		}
		tag.UsageCount.Value += target.Length;
		await transaction.CommitAsync();
	}

	public async Task RemoveTagAsync(IFileModel[] fileModels, int tagId) {
		var target = fileModels.Where(x => x.Tags.Any(t => t.TagId == tagId)).ToArray();
		if (!target.Any()) {
			return;
		}

		var ids = target.Select(x => x.Id);
		await using var db = await this._dbFactory.CreateDbContextAsync();
		using var transaction = await db.Database.BeginTransactionAsync();
		var rel =
			await
				db
					.MediaFileTags
					.Where(x => ids.Contains(x.MediaFileId) && x.Tag.TagId == tagId)
					.ToArrayAsync();
		if (rel.Any()) {
			db.MediaFileTags.RemoveRange(rel);
			await db.SaveChangesAsync();

			var removedIds = rel.Select(x => x.MediaFileId).ToHashSet();
			foreach (var file in target) {
				if (removedIds.Contains(file.Id)) {
					file.Tags.RemoveAll(x => x.TagId == tagId);
				}
			}
			var tag = this.Tags.FirstOrDefault(x => x.TagId == tagId);
			tag?.UsageCount.Value -= rel.Length;
			await transaction.CommitAsync();
		}
	}

	public async Task UpdateTagAsync(int tagId, int? tagCategoryId, string tagName, string detail, IEnumerable<ITagAliasModel> aliases) {
		await using var db = await this._dbFactory.CreateDbContextAsync();
		using var transaction = await db.Database.BeginTransactionAsync();
		var tag = await db.Tags.FirstAsync(x => x.TagId == tagId);
		tag.TagCategoryId = tagCategoryId;
		tag.TagName = tagName;
		tag.Detail = detail;
		db.Tags.Update(tag);

		db.TagAliases.RemoveRange(db.TagAliases.Where(x => x.TagId == tagId));
		await db.TagAliases.AddRangeAsync(aliases.Select((x, i) => new TagAlias { TagId = tagId, TagAliasId = i, Alias = x.Alias, Ruby = x.Ruby }));

		await db.SaveChangesAsync();
		await transaction.CommitAsync();

		var cachedTag = this.Tags.FirstOrDefault(x => x.TagId == tagId);
		if (cachedTag != null) {
			var oldCategoryId = cachedTag.TagCategoryId;
			cachedTag.TagCategoryId = tagCategoryId;
			cachedTag.TagName = tagName;
			cachedTag.Detail = detail;
			cachedTag.Romaji = tagName.KatakanaToHiragana().HiraganaToRomaji();
			cachedTag.TagAliases = [.. aliases];

			if (oldCategoryId != tagCategoryId) {
				var oldCategory = this.TagCategories.FirstOrDefault(x => x.TagCategoryId == oldCategoryId);
				oldCategory?.Tags.Remove(cachedTag);

				var newCategory = this.TagCategories.FirstOrDefault(x => x.TagCategoryId == tagCategoryId) ?? this.TagCategories.First(x => x.TagCategoryId == null);
				newCategory.Tags.Add(cachedTag);
				cachedTag.TagCategory = newCategory;
			}
		}
	}

	public async Task<ITagCategoryModel> CreateTagCategoryAsync(string tagCategoryName, string detail) {
		await using var db = await this._dbFactory.CreateDbContextAsync();
		using var transaction = await db.Database.BeginTransactionAsync();

		var tagCategoryEntity = new TagCategory() { TagCategoryName = tagCategoryName, Detail = detail, Tags = [] };
		await db.TagCategories.AddAsync(tagCategoryEntity);

		await db.SaveChangesAsync();
		await transaction.CommitAsync();

		var newCategory = this._tagModelFactory.CreateCategory(tagCategoryEntity);
		this.TagCategories.Add(newCategory);
		return newCategory;
	}

	public async Task UpdateTagCategoryAsync(int tagCategoryId, string tagCategoryName, string detail) {
		await using var db = await this._dbFactory.CreateDbContextAsync();
		using var transaction = await db.Database.BeginTransactionAsync();

		var tagCategoryEntity = await db.TagCategories.FirstOrDefaultAsync(x => x.TagCategoryId == tagCategoryId) ?? throw new InvalidOperationException($"TagCategory with ID {tagCategoryId} not found.");

		tagCategoryEntity.TagCategoryName = tagCategoryName;
		tagCategoryEntity.Detail = detail;
		db.TagCategories.Update(tagCategoryEntity);

		await db.SaveChangesAsync();
		await transaction.CommitAsync();

		var cachedCategory = this.TagCategories.FirstOrDefault(x => x.TagCategoryId == tagCategoryId);
		if (cachedCategory != null) {
			cachedCategory.TagCategoryName = tagCategoryName;
			cachedCategory.Detail = detail;
		}
	}

	public async Task InitializeAsync() {
		if (this._isInitialized) {
			return;
		}
		this._isInitialized = true;

		await using var db = await this._dbFactory.CreateDbContextAsync();
		var tagCategories =
			await
				db.TagCategories
					.AsSplitQuery()
					.Include(x => x.Tags)
					.ThenInclude(x => x.TagAliases)
					.Include(x => x.Tags)
					.ThenInclude(x => x.MediaFileTags)
					.OrderBy(x => x.TagCategoryId)
					.ToArrayAsync();

		var noCategoryTags =
			await
				db.Tags
					.AsSplitQuery()
					.Where(x => x.TagCategoryId == null)
					.Include(x => x.TagAliases)
					.Include(x => x.MediaFileTags)
					.OrderByDescending(x => x.MediaFileTags.Count)
					.ToArrayAsync();

		this.TagCategories.Clear();
		this.Tags.Clear();

		var tags = new List<ITagModel>();

		var noCategoryModel = this._tagModelFactory.CreateCategory(null);
		noCategoryModel.Tags.AddRange(noCategoryTags.Select(t => this._tagModelFactory.Create(t, noCategoryModel)).OrderByDescending(x => x.UsageCount.Value));
		this.TagCategories.Add(noCategoryModel);
		tags.AddRange(noCategoryModel.Tags);

		foreach (var categoryEntity in tagCategories) {
			var categoryModel = this._tagModelFactory.CreateCategory(categoryEntity);
			var list = categoryModel.Tags.OrderByDescending(x => x.UsageCount.Value).ToArray();
			categoryModel.Tags.Clear();
			categoryModel.Tags.AddRange(list);
			this.TagCategories.Add(categoryModel);
			tags.AddRange(categoryModel.Tags);
		}

		this.Tags.AddRange(tags);
	}
}