using MediaDeck.Common.Base;
using MediaDeck.Composition.Database;
using MediaDeck.Composition.Interfaces.MediaItemTypes.Models;
using MediaDeck.Composition.Interfaces.Tags;
using MediaDeck.Composition.Tables;

namespace MediaDeck.Core.Models.Tags;

/// <summary>
/// タグおよびタグカテゴリーの永続化とインメモリ状態を管理するマネージャークラス。
/// </summary>
[Inject(InjectServiceLifetime.Singleton, typeof(ITagsManager))]
public class TagsManager : ModelBase, ITagsManager {
	private readonly IDbContextFactory<MediaDeckDbContext> _dbFactory;
	private readonly ITagModelFactory _tagModelFactory;
	private readonly Subject<ITagModel> _tagChanged;
	private bool _isInitialized;

	private readonly ObservableList<ITagCategoryModel> _tagCategories = [];
	private readonly ObservableList<ITagModel> _tags = [];

	/// <summary>
	/// <see cref="TagsManager"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	/// <param name="dbFactory">DBコンテキストファクトリ</param>
	/// <param name="tagModelFactory">タグモデルファクトリ</param>
	public TagsManager(IDbContextFactory<MediaDeckDbContext> dbFactory, ITagModelFactory tagModelFactory) {
		this._dbFactory = dbFactory;
		this._tagModelFactory = tagModelFactory;
		this._tagChanged = new Subject<ITagModel>().AddTo(this.CompositeDisposable);
	}

	/// <inheritdoc />
	public IReadOnlyObservableList<ITagCategoryModel> TagCategories {
		get {
			return this._tagCategories;
		}
	}

	/// <inheritdoc />
	public IReadOnlyObservableList<ITagModel> Tags {
		get {
			return this._tags;
		}
	}

	/// <inheritdoc />
	public Observable<ITagModel> TagChanged {
		get {
			return this._tagChanged.AsObservable();
		}
	}

	/// <inheritdoc />
	public ITagCategoryModel NoCategory {
		get;
		private set;
	} = null!;

	/// <inheritdoc />
	public async Task<ITagModel?> FindTagByNameAsync(string tagName) {
		var tag = this._tags.FirstOrDefault(x => x.TagName == tagName);
		return await Task.FromResult(tag);
	}

	/// <inheritdoc />
	public async Task<ITagCategoryModel> CreateTagCategoryAsync(string name = "", string detail = "") {
		await using var db = await this._dbFactory.CreateDbContextAsync();
		var entity = new TagCategory {
			TagCategoryName = name,
			Detail = detail,
			Tags = []
		};
		await db.TagCategories.AddAsync(entity);
		await db.SaveChangesAsync();

		var model = this._tagModelFactory.CreateCategory(entity);
		this._tagCategories.Add(model);
		return model;
	}

	/// <inheritdoc />
	public async Task UpdateTagCategoryAsync(ITagCategoryModel categoryModel, string name, string detail) {
		if (categoryModel == this.NoCategory || categoryModel.TagCategoryId == null) {
			return;
		}

		await using var db = await this._dbFactory.CreateDbContextAsync();
		var entity = await db.TagCategories.FirstOrDefaultAsync(x => x.TagCategoryId == categoryModel.TagCategoryId);
		if (entity != null) {
			entity.TagCategoryName = name;
			entity.Detail = detail;
			await db.SaveChangesAsync();
		}

		categoryModel.TagCategoryName = name;
		categoryModel.Detail = detail;
		categoryModel.NotifyChanged();
	}

	/// <inheritdoc />
	public async Task DeleteTagCategoryAsync(ITagCategoryModel categoryModel) {
		if (categoryModel == this.NoCategory) {
			return;
		}

		if (categoryModel.TagCategoryId != null) {
			var categoryId = categoryModel.TagCategoryId.Value;
			await using var db = await this._dbFactory.CreateDbContextAsync();
			using var transaction = await db.Database.BeginTransactionAsync();

			var tagsInCategory = await db.Tags.Where(x => x.TagCategoryId == categoryId).ToArrayAsync();
			foreach (var tagEntity in tagsInCategory) {
				tagEntity.TagCategoryId = null;
			}

			var entity = await db.TagCategories.FirstOrDefaultAsync(x => x.TagCategoryId == categoryId);
			if (entity != null) {
				db.TagCategories.Remove(entity);
			}

			await db.SaveChangesAsync();
			await transaction.CommitAsync();
		}

		this._tagCategories.Remove(categoryModel);

		var targetTags = categoryModel.Tags.ToArray();
		foreach (var targetTag in targetTags) {
			targetTag.TagCategoryId = null;
			targetTag.TagCategory = this.NoCategory;
			this.NoCategory.AddTag(targetTag);
			targetTag.NotifyChanged();
			this._tagChanged.OnNext(targetTag);
		}
		categoryModel.ClearTags();
		categoryModel.Dispose();
	}

	/// <inheritdoc />
	public async Task<ITagModel?> CreateTagImmediatelyAsync(int? tagCategoryId, string tagName, string? ruby, string detail, IEnumerable<ITagAliasModel> aliases) {
		await using var db = await this._dbFactory.CreateDbContextAsync();
		using var transaction = await db.Database.BeginTransactionAsync();
		var tag = new Tag {
			TagCategoryId = tagCategoryId,
			TagName = tagName,
			Ruby = string.IsNullOrEmpty(ruby) ? null : ruby,
			Detail = detail,
			TagAliases = [],
			MediaItemTags = [],
			TagCategory = null
		};
		await db.AddAsync(tag);
		await db.SaveChangesAsync();
		await db.Entry(tag).Reference(x => x.TagCategory).LoadAsync();

		var aliasList = aliases.Select((x, i) => new TagAlias {
			TagId = tag.TagId,
			TagAliasId = i,
			Alias = x.Alias,
			Ruby = string.IsNullOrEmpty(x.Ruby) ? null : x.Ruby
		}).ToArray();

		if (aliasList.Length > 0) {
			await db.TagAliases.AddRangeAsync(aliasList);
			await db.SaveChangesAsync();
		}

		await transaction.CommitAsync();

		var categoryModel = this._tagCategories.FirstOrDefault(x => x.TagCategoryId == tagCategoryId)
			?? this._tagCategories.First(x => x.TagCategoryId == null);
		var newTagModel = this._tagModelFactory.Create(tag, categoryModel);
		categoryModel.AddTag(newTagModel);
		this._tags.Add(newTagModel);
		return newTagModel;
	}

	/// <inheritdoc />
	public async Task UpdateTagAsync(ITagModel tagModel, ITagCategoryModel targetCategory, string tagName, string? ruby, string detail, IEnumerable<ITagAliasModel> aliases) {
		var resolvedCategory = this._tagCategories.Contains(targetCategory) ? targetCategory : this.NoCategory;
		var targetCategoryId = resolvedCategory.TagCategoryId;
		var normalizedRuby = string.IsNullOrEmpty(ruby) ? null : ruby;
		var aliasEntities = aliases.Select((x, i) => new TagAlias {
			TagId = tagModel.TagId,
			TagAliasId = i,
			Alias = x.Alias,
			Ruby = string.IsNullOrEmpty(x.Ruby) ? null : x.Ruby
		}).ToArray();

		await using var db = await this._dbFactory.CreateDbContextAsync();
		using var transaction = await db.Database.BeginTransactionAsync();
		var tagEntity = await db.Tags.Include(x => x.TagAliases).FirstOrDefaultAsync(x => x.TagId == tagModel.TagId);
		if (tagEntity != null) {
			tagEntity.TagCategoryId = targetCategoryId;
			tagEntity.TagName = tagName;
			tagEntity.Ruby = normalizedRuby;
			tagEntity.Detail = detail;

			db.TagAliases.RemoveRange(tagEntity.TagAliases);
			if (aliasEntities.Length > 0) {
				await db.TagAliases.AddRangeAsync(aliasEntities);
			}

			await db.SaveChangesAsync();
		}
		await transaction.CommitAsync();

		tagModel.TagName = tagName;
		tagModel.Ruby = normalizedRuby;
		tagModel.Detail = detail;
		tagModel.TagCategoryId = targetCategoryId;
		tagModel.TagAliases = [.. aliasEntities.Select(this._tagModelFactory.CreateAlias)];

		if (tagModel.TagCategory != resolvedCategory) {
			tagModel.TagCategory.RemoveTag(tagModel);
			resolvedCategory.AddTag(tagModel);
			tagModel.TagCategory = resolvedCategory;
		}

		tagModel.NotifyChanged();
		this._tagChanged.OnNext(tagModel);
	}

	/// <inheritdoc />
	public async Task DeleteTagAsync(ITagModel tagModel) {
		await using var db = await this._dbFactory.CreateDbContextAsync();
		var entities = await db.Tags.Where(x => x.TagId == tagModel.TagId).ToArrayAsync();
		if (entities.Length > 0) {
			db.Tags.RemoveRange(entities);
			await db.SaveChangesAsync();
		}

		this._tags.Remove(tagModel);
		foreach (var categoryModel in this._tagCategories) {
			categoryModel.RemoveTag(tagModel);
		}
		tagModel.Dispose();
	}

	/// <inheritdoc />
	public async Task AddTagAsync(IMediaItemModel[] fileModels, ITagModel tag) {
		if (tag == null) {
			return;
		}

		var target = fileModels.Where(x => !x.Tags.Any(t => t.TagId == tag.TagId)).ToArray();
		if (target.Length == 0) {
			return;
		}

		await using var db = await this._dbFactory.CreateDbContextAsync();
		using var transaction = await db.Database.BeginTransactionAsync();
		await db.MediaItemTags.AddRangeAsync(target.Select(x => new MediaItemTag { MediaItemId = x.Id, TagId = tag.TagId }));
		await db.SaveChangesAsync();
		foreach (var file in target) {
			file.Tags.Add(tag);
		}
		tag.UsageCount.Value += target.Length;
		await transaction.CommitAsync();
	}

	/// <inheritdoc />
	public async Task RemoveTagAsync(IMediaItemModel[] fileModels, int tagId) {
		var target = fileModels.Where(x => x.Tags.Any(t => t.TagId == tagId)).ToArray();
		if (target.Length == 0) {
			return;
		}

		var ids = target.Select(x => x.Id);
		await using var db = await this._dbFactory.CreateDbContextAsync();
		using var transaction = await db.Database.BeginTransactionAsync();
		var rel =
			await
				db
					.MediaItemTags
					.Where(x => ids.Contains(x.MediaItemId) && x.Tag.TagId == tagId)
					.ToArrayAsync();
		if (rel.Length > 0) {
			db.MediaItemTags.RemoveRange(rel);
			await db.SaveChangesAsync();

			var removedIds = rel.Select(x => x.MediaItemId).ToHashSet();
			foreach (var file in target) {
				if (removedIds.Contains(file.Id)) {
					file.Tags.RemoveAll(x => x.TagId == tagId);
				}
			}
			var tag = this._tags.FirstOrDefault(x => x.TagId == tagId);
			tag?.UsageCount.Value -= rel.Length;
			await transaction.CommitAsync();
		}
	}

	/// <inheritdoc />
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
					.ThenInclude(x => x.MediaItemTags)
					.OrderBy(x => x.TagCategoryId)
					.ToArrayAsync();

		var noCategoryTags =
			await
				db.Tags
					.AsSplitQuery()
					.Where(x => x.TagCategoryId == null)
					.Include(x => x.TagAliases)
					.Include(x => x.MediaItemTags)
					.OrderByDescending(x => x.MediaItemTags.Count)
					.ToArrayAsync();

		this._tagCategories.Clear();
		this._tags.Clear();

		var tags = new List<ITagModel>();

		var noCategoryModel = this._tagModelFactory.CreateCategory(null);
		this.NoCategory = noCategoryModel;
		noCategoryModel.AddTagRange(noCategoryTags.Select(t => this._tagModelFactory.Create(t, noCategoryModel)));
		this._tagCategories.Add(noCategoryModel);
		tags.AddRange(noCategoryModel.Tags);

		foreach (var categoryEntity in tagCategories) {
			var categoryModel = this._tagModelFactory.CreateCategory(categoryEntity);
			this._tagCategories.Add(categoryModel);
			tags.AddRange(categoryModel.Tags);
		}

		this._tags.AddRange(tags);
	}
}