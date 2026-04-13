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
			MediaFileTags = [],
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

	public async Task SaveAsync() {
		await using var db = await this._dbFactory.CreateDbContextAsync();
		using var transaction = await db.Database.BeginTransactionAsync();
		try {
			foreach (var category in this.TagCategories) {
				int? dbCategoryId = category.TagCategoryId;

				if (dbCategoryId == null && category.TagCategoryName != "未設定") {
					// 新規カテゴリーの作成
					var entity = new TagCategory {
						TagCategoryName = category.TagCategoryName,
						Detail = category.Detail,
						Tags = []
					};
					await db.TagCategories.AddAsync(entity);
					await db.SaveChangesAsync();
					category.TagCategoryId = entity.TagCategoryId;
					dbCategoryId = entity.TagCategoryId;
				} else if (dbCategoryId != null && category.IsDirty) {
					// 既存カテゴリーの更新
					var entity = await db.TagCategories.FirstOrDefaultAsync(x => x.TagCategoryId == dbCategoryId);
					if (entity != null) {
						entity.TagCategoryName = category.TagCategoryName;
						entity.Detail = category.Detail;
						db.TagCategories.Update(entity);
					}
				}

				// カテゴリーに属するタグの保存
				foreach (var tag in category.Tags) {
					if (!tag.IsDirty) {
						continue;
					}

					Tag? tagEntity = null;
					try {
						var tagId = tag.TagId;
						tagEntity = await db.Tags.Include(x => x.TagAliases).FirstOrDefaultAsync(x => x.TagId == tagId);
					} catch (InvalidOperationException) {
						// ID が未初期化の場合は新規作成とみなす
					}

					if (tagEntity == null) {
						// 新規タグの作成
						tagEntity = new Tag {
							TagCategoryId = dbCategoryId,
							TagName = tag.TagName,
							Detail = tag.Detail,
							TagAliases = []
						};
						await db.Tags.AddAsync(tagEntity);
						await db.SaveChangesAsync();
						tag.TagId = tagEntity.TagId;
					} else {
						// 既存タグの更新
						tagEntity.TagCategoryId = dbCategoryId;
						tagEntity.TagName = tag.TagName;
						tagEntity.Detail = tag.Detail;
						db.Tags.Update(tagEntity);
					}

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

			// フラグのリセット
			foreach (var category in this.TagCategories) {
				category.IsDirty = false;
				foreach (var tag in category.Tags) {
					tag.IsDirty = false;
				}
			}

			// TODO: 全タグリスト (this.Tags) との整合性も必要に応じて整える
		} catch {
			await transaction.RollbackAsync();
			throw;
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