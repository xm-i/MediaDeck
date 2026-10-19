using MediaDeck.Composition.Database;
using MediaDeck.Composition.Interfaces.MediaItemTypes.Models;
using MediaDeck.Composition.Interfaces.Tags;
using MediaDeck.Composition.Tables;

namespace MediaDeck.Core.Models.Tags;

[Inject(InjectServiceLifetime.Singleton, typeof(ITagsManager))]
public class TagsManager(IDbContextFactory<MediaDeckDbContext> dbFactory, ITagModelFactory tagModelFactory) : ITagsManager {
	private readonly IDbContextFactory<MediaDeckDbContext> _dbFactory = dbFactory;
	private readonly ITagModelFactory _tagModelFactory = tagModelFactory;
	private bool _isInitialized;

	private readonly List<ITagCategoryModel> _pendingDeleteCategories = [];
	private readonly List<ITagModel> _pendingDeleteTags = [];

	private readonly ObservableList<ITagCategoryModel> _tagCategories = [];
	private readonly ObservableList<ITagModel> _tags = [];

	public IReadOnlyObservableList<ITagCategoryModel> TagCategories {
		get {
			return this._tagCategories;
		}
	}

	public IReadOnlyObservableList<ITagModel> Tags {
		get {
			return this._tags;
		}
	}

	public ITagCategoryModel NoCategory {
		get;
		private set;
	} = null!;

	public async Task<ITagModel?> FindTagByNameAsync(string tagName) {
		var tag = this._tags.FirstOrDefault(x => x.TagName == tagName);
		return tag;
	}

	/// <inheritdoc />
	public ITagCategoryModel AddTagCategory() {
		var model = this._tagModelFactory.CreateCategory();
		model.TagCategoryName = "";
		model.Detail = "";
		this._tagCategories.Add(model);
		return model;
	}

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

		var aliasList = aliases.Select((x, i) => new TagAlias { TagId = tag.TagId, TagAliasId = i, Alias = x.Alias, Ruby = x.Ruby }).ToArray();

		if (aliasList.Length > 0) {
			await db.TagAliases.AddRangeAsync(aliasList);
			await db.SaveChangesAsync();
		}

		await transaction.CommitAsync();

		var categoryModel = this._tagCategories.FirstOrDefault(x => x.TagCategoryId == tagCategoryId) ?? this._tagCategories.First(x => x.TagCategoryId == null);
		var newTagModel = this._tagModelFactory.Create(tag, categoryModel);
		categoryModel.AddTag(newTagModel);
		this._tags.Add(newTagModel);
		return newTagModel;
	}

	public async Task AddTagAsync(IMediaItemModel[] fileModels, ITagModel tag) {
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

		await db.MediaItemTags.AddRangeAsync(target.Select(x => new MediaItemTag { MediaItemId = x.Id, TagId = tag.TagId }));
		await db.SaveChangesAsync();
		foreach (var file in target) {
			file.Tags.Add(tag);
		}
		tag.UsageCount.Value += target.Length;
		await transaction.CommitAsync();
	}

	public async Task RemoveTagAsync(IMediaItemModel[] fileModels, int tagId) {
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
					.MediaItemTags
					.Where(x => ids.Contains(x.MediaItemId) && x.Tag.TagId == tagId)
					.ToArrayAsync();
		if (rel.Any()) {
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

	public async Task DeleteTagCategoryAsync(ITagCategoryModel categoryModel) {
		this._tagCategories.Remove(categoryModel);

		// UI上のタグを未設定カテゴリへ移動
		var targetTags = categoryModel.Tags.ToArray();
		foreach (var targetTag in targetTags) {
			targetTag.TagCategoryId = null;
			targetTag.TagCategory = this.NoCategory;
			this.NoCategory.AddTag(targetTag);
		}
		categoryModel.ClearTags();

		// DBに存在する（IDがある）場合は、保存時に削除するためリストに保持
		if (categoryModel.TagCategoryId != null) {
			this._pendingDeleteCategories.Add(categoryModel);
		}

		await Task.CompletedTask;
	}

	public async Task DeleteTagAsync(ITagModel tagModel) {
		this._tags.Remove(tagModel);
		// 所属カテゴリから除去する（未保存の新規カテゴリは ID が null で未分類と区別できないため、ID ではなく全カテゴリを対象にする）
		foreach (var categoryModel in this._tagCategories) {
			categoryModel.RemoveTag(tagModel);
		}

		// DBに存在する（採番済みの）タグは、保存時に削除するためリストに保持
		if (!tagModel.IsNew) {
			this._pendingDeleteTags.Add(tagModel);
		}

		await Task.CompletedTask;
	}

	public async Task SaveAsync() {
		await using var db = await this._dbFactory.CreateDbContextAsync();
		using var transaction = await db.Database.BeginTransactionAsync();
		try {
			// 1. 削除待ちのタグを削除
			if (this._pendingDeleteTags.Any()) {
				var tagIds = this._pendingDeleteTags.Select(x => x.TagId).ToArray();
				var entities = await db.Tags.Where(x => tagIds.Contains(x.TagId)).ToArrayAsync();
				db.Tags.RemoveRange(entities);
				await db.SaveChangesAsync();
			}

			// 2. 削除待ちのカテゴリを削除
			if (this._pendingDeleteCategories.Any()) {
				var categoryIds = this._pendingDeleteCategories.Select(x => x.TagCategoryId).ToArray();
				var entities = await db.TagCategories.Where(x => categoryIds.Contains(x.TagCategoryId)).ToArrayAsync();
				db.TagCategories.RemoveRange(entities);
				await db.SaveChangesAsync();
			}

			// 3. カテゴリの保存（新規カテゴリはここで採番し、後続のタグ保存で ID を参照できるようにする）
			foreach (var category in this._tagCategories) {
				if (category == this.NoCategory) {
					// システム定義の未分類カテゴリは DB エンティティを持たない
					continue;
				}
				if (category.TagCategoryId == null) {
					// 新規カテゴリーの作成
					var entity = new TagCategory {
						TagCategoryName = category.TagCategoryName,
						Detail = category.Detail,
						Tags = []
					};
					await db.TagCategories.AddAsync(entity);
					await db.SaveChangesAsync();
					category.TagCategoryId = entity.TagCategoryId;
				} else if (category.IsDirty) {
					// 既存カテゴリーの更新
					var categoryId = category.TagCategoryId;
					var entity = await db.TagCategories.FirstOrDefaultAsync(x => x.TagCategoryId == categoryId);
					if (entity != null) {
						entity.TagCategoryName = category.TagCategoryName;
						entity.Detail = category.Detail;
						db.TagCategories.Update(entity);
					}
				}
			}

			// 4. タグの保存（所属カテゴリは TagCategory 参照を正とする）
			foreach (var category in this._tagCategories) {
				foreach (var tag in category.Tags) {
					if (!tag.IsDirty) {
						continue;
					}

					var targetCategoryId = this.ResolveTargetCategory(tag).TagCategoryId;

					Tag? tagEntity = null;
					if (!tag.IsNew) {
						var tagId = tag.TagId;
						tagEntity = await db.Tags.Include(x => x.TagAliases).FirstOrDefaultAsync(x => x.TagId == tagId);
					}

					if (tagEntity == null) {
						// 新規タグの作成
						tagEntity = new Tag {
							TagCategoryId = targetCategoryId,
							TagName = tag.TagName,
							Ruby = tag.Ruby,
							Detail = tag.Detail,
							TagAliases = []
						};
						await db.Tags.AddAsync(tagEntity);
						await db.SaveChangesAsync();
						tag.TagId = tagEntity.TagId;
					} else {
						// 既存タグの更新
						tagEntity.TagCategoryId = targetCategoryId;
						tagEntity.TagName = tag.TagName;
						tagEntity.Ruby = tag.Ruby;
						tagEntity.Detail = tag.Detail;
						db.Tags.Update(tagEntity);
					}
					tag.TagCategoryId = targetCategoryId;

					// 別名の更新 (一括削除して再追加)
					db.TagAliases.RemoveRange(tagEntity.TagAliases);
					foreach (var (alias, index) in tag.TagAliases.Select((x, i) => (x, i))) {
						await db.TagAliases.AddAsync(new TagAlias {
							TagId = tagEntity.TagId,
							TagAliasId = index,
							Alias = alias.Alias,
							Ruby = string.IsNullOrEmpty(alias.Ruby) ? null : alias.Ruby
						});
					}
				}
			}

			await db.SaveChangesAsync();
			await transaction.CommitAsync();

			// 5. カテゴリ移動をインメモリのカテゴリ別リストへ反映
			var tagsToMove = new List<(ITagModel Tag, ITagCategoryModel OldCategory, ITagCategoryModel NewCategory)>();
			foreach (var category in this._tagCategories) {
				foreach (var tag in category.Tags) {
					var targetCategory = this.ResolveTargetCategory(tag);
					if (targetCategory != category) {
						tagsToMove.Add((tag, category, targetCategory));
					}
				}
			}
			foreach (var (tag, oldCategory, newCategory) in tagsToMove) {
				oldCategory.RemoveTag(tag);
				newCategory.AddTag(tag);
				tag.TagCategory = newCategory;
			}

			// 6. カテゴリ側にのみ存在するタグ（新規作成分）を全タグリスト (this._tags) へ反映
			var knownTags = this._tags.ToHashSet();
			foreach (var category in this._tagCategories) {
				foreach (var tag in category.Tags) {
					if (knownTags.Add(tag)) {
						this._tags.Add(tag);
					}
				}
			}

			// フラグのリセット
			foreach (var category in this._tagCategories) {
				category.IsDirty = false;
				foreach (var tag in category.Tags) {
					tag.IsDirty = false;
				}
			}

			// 完了後にリストをクリア
			this._pendingDeleteTags.Clear();
			this._pendingDeleteCategories.Clear();
		} catch {
			await transaction.RollbackAsync();
			throw;
		}
	}

	/// <summary>
	/// タグの保存先となるカテゴリを解決します。
	/// タグの <see cref="ITagModel.TagCategory"/> 参照を正とし、管理対象外（削除済み等）の場合は未分類カテゴリを返します。
	/// </summary>
	/// <param name="tag">対象のタグ</param>
	/// <returns>保存先のカテゴリ</returns>
	private ITagCategoryModel ResolveTargetCategory(ITagModel tag) {
		var category = tag.TagCategory;
		return this._tagCategories.Contains(category) ? category : this.NoCategory;
	}

	public async Task ReloadAsync() {
		this._isInitialized = false;
		await this.InitializeAsync();
	}

	public async Task InitializeAsync() {
		if (this._isInitialized) {
			return;
		}
		this._isInitialized = true;
		this._pendingDeleteTags.Clear();
		this._pendingDeleteCategories.Clear();

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
		noCategoryModel.ClearTags();
		noCategoryModel.AddTagRange(noCategoryTags.Select(t => this._tagModelFactory.Create(t, noCategoryModel)).OrderByDescending(x => x.UsageCount.Value));
		this._tagCategories.Add(noCategoryModel);
		tags.AddRange(noCategoryModel.Tags);

		foreach (var categoryEntity in tagCategories) {
			var categoryModel = this._tagModelFactory.CreateCategory(categoryEntity);
			var list = categoryModel.Tags.OrderByDescending(x => x.UsageCount.Value).ToArray();
			categoryModel.ClearTags();
			categoryModel.AddTagRange(list);
			this._tagCategories.Add(categoryModel);
			tags.AddRange(categoryModel.Tags);
		}

		this._tags.AddRange(tags);
	}
}