using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.MediaItemTypes.Models;
using MediaDeck.Composition.Interfaces.Tags;
using MediaDeck.Composition.Tables;
using MediaDeck.Core.Models.Tags;
using Microsoft.EntityFrameworkCore;
using Moq;
using R3;
using Shouldly;

namespace MediaDeck.Core.Tests.Models.Tags;

/// <summary>
/// <see cref="TagsManager"/> の単体テスト。
/// </summary>
/// <summary>
/// <see cref="TagsManager"/> の単体テスト。
/// </summary>
public sealed class TagsManagerTests : IDisposable {
	private readonly List<SqliteTestDbFactory> _dbFactories = [];

	/// <summary>
	/// テストで使用したデータベースファクトリを解放します。
	/// </summary>
	public void Dispose() {
		foreach (var factory in this._dbFactories) {
			factory.Dispose();
		}
	}

	/// <summary>
	/// テスト用の SQLite インメモリデータベースファクトリを生成します。
	/// </summary>
	/// <returns>テスト用データベースファクトリ</returns>
	private SqliteTestDbFactory CreateSqliteDbFactory() {
		var factory = new SqliteTestDbFactory();
		this._dbFactories.Add(factory);
		return factory;
	}

	private async Task SeedDatabaseAsync(SqliteTestDbFactory dbFactory) {
		await using var db = await dbFactory.CreateDbContextAsync();
		db.TagCategories.Add(new TagCategory { TagCategoryId = 1, TagCategoryName = "Category1", Detail = "CatDetail1", Tags = [] });
		db.TagCategories.Add(new TagCategory { TagCategoryId = 2, TagCategoryName = "Category2", Detail = "CatDetail2", Tags = [] });
		db.Tags.Add(new Tag { TagId = 1, TagCategoryId = 1, TagName = "Tag1", Detail = "Detail1", TagCategory = null!, MediaItemTags = [], TagAliases = [] });
		db.Tags.Add(new Tag { TagId = 2, TagCategoryId = 1, TagName = "Tag2", Detail = "Detail2", TagCategory = null!, MediaItemTags = [], TagAliases = [] });
		db.TagAliases.Add(new TagAlias { TagId = 1, TagAliasId = 1, Alias = "Alias1" });
		await db.SaveChangesAsync();
	}

	[Fact]
	public async Task FindTagByNameAsync_ShouldReturnTag_WhenTagExists() {
		var dbFactory = this.CreateSqliteDbFactory();
		await using (var db = await dbFactory.CreateDbContextAsync()) {
			db.Tags.Add(new Tag { TagId = 1, TagName = "ExistingTag", Detail = "", TagCategory = null!, MediaItemTags = [], TagAliases = [] });
			await db.SaveChangesAsync();
		}
		var manager = new TagsManager(dbFactory, new TagModelFactory());
		await manager.InitializeAsync();
		var result = await manager.FindTagByNameAsync("ExistingTag");
		result.ShouldNotBeNull();
		result.TagName.ShouldBe("ExistingTag");
	}

	[Fact]
	public async Task FindTagByNameAsync_ShouldReturnNull_WhenTagDoesNotExist() {
		var dbFactory = this.CreateSqliteDbFactory();
		var manager = new TagsManager(dbFactory, new TagModelFactory());
		var result = await manager.FindTagByNameAsync("NonExistingTag");
		result.ShouldBeNull();
	}

	[Fact]
	public async Task CreateTagCategoryAsync_ShouldCreateAndPersistCategoryImmediately() {
		var dbFactory = this.CreateSqliteDbFactory();
		await this.SeedDatabaseAsync(dbFactory);
		var manager = new TagsManager(dbFactory, new TagModelFactory());
		await manager.InitializeAsync();

		var created = await manager.CreateTagCategoryAsync("NewCat", "NewCatDetail");

		created.TagCategoryId.ShouldNotBeNull();
		created.TagCategoryName.ShouldBe("NewCat");
		created.Detail.ShouldBe("NewCatDetail");
		manager.TagCategories.ShouldContain(created);

		await using var db = await dbFactory.CreateDbContextAsync();
		var dbCat = await db.TagCategories.FirstOrDefaultAsync(x => x.TagCategoryId == created.TagCategoryId);
		dbCat.ShouldNotBeNull();
		dbCat.TagCategoryName.ShouldBe("NewCat");
		dbCat.Detail.ShouldBe("NewCatDetail");
	}

	[Fact]
	public async Task UpdateTagCategoryAsync_ShouldUpdateAndPersistCategory() {
		var dbFactory = this.CreateSqliteDbFactory();
		await this.SeedDatabaseAsync(dbFactory);
		var manager = new TagsManager(dbFactory, new TagModelFactory());
		await manager.InitializeAsync();

		var cat1 = manager.TagCategories.First(x => x.TagCategoryId == 1);
		var notified = false;
		using var sub = cat1.Changed.Subscribe(_ => notified = true);

		await manager.UpdateTagCategoryAsync(cat1, "UpdatedCat1", "UpdatedDetail1");

		cat1.TagCategoryName.ShouldBe("UpdatedCat1");
		cat1.Detail.ShouldBe("UpdatedDetail1");
		notified.ShouldBeTrue();

		await using var db = await dbFactory.CreateDbContextAsync();
		var dbCat1 = await db.TagCategories.FirstAsync(x => x.TagCategoryId == 1);
		dbCat1.TagCategoryName.ShouldBe("UpdatedCat1");
		dbCat1.Detail.ShouldBe("UpdatedDetail1");
	}

	[Fact]
	public async Task UpdateTagCategoryAsync_ShouldDoNothing_ForNoCategory() {
		var dbFactory = this.CreateSqliteDbFactory();
		await this.SeedDatabaseAsync(dbFactory);
		var manager = new TagsManager(dbFactory, new TagModelFactory());
		await manager.InitializeAsync();

		var originalName = manager.NoCategory.TagCategoryName;
		await manager.UpdateTagCategoryAsync(manager.NoCategory, "Modified", "ModifiedDetail");

		manager.NoCategory.TagCategoryName.ShouldBe(originalName);
	}

	[Fact]
	public async Task DeleteTagCategoryAsync_ShouldDeleteImmediatelyAndMoveTagsToNoCategory() {
		var dbFactory = this.CreateSqliteDbFactory();
		await this.SeedDatabaseAsync(dbFactory);
		var manager = new TagsManager(dbFactory, new TagModelFactory());
		await manager.InitializeAsync();

		var cat1 = manager.TagCategories.First(x => x.TagCategoryId == 1);
		var tag1 = cat1.Tags.First(x => x.TagId == 1);

		await manager.DeleteTagCategoryAsync(cat1);

		manager.TagCategories.ShouldNotContain(cat1);
		manager.NoCategory.Tags.ShouldContain(tag1);
		tag1.TagCategory.ShouldBe(manager.NoCategory);
		tag1.TagCategoryId.ShouldBeNull();

		await using var db = await dbFactory.CreateDbContextAsync();
		var dbCat = await db.TagCategories.FirstOrDefaultAsync(x => x.TagCategoryId == 1);
		dbCat.ShouldBeNull();
		var dbTag = await db.Tags.FirstAsync(x => x.TagId == 1);
		dbTag.TagCategoryId.ShouldBeNull();
	}

	[Fact]
	public async Task DeleteTagCategoryAsync_ShouldDoNothing_ForNoCategory() {
		var dbFactory = this.CreateSqliteDbFactory();
		await this.SeedDatabaseAsync(dbFactory);
		var manager = new TagsManager(dbFactory, new TagModelFactory());
		await manager.InitializeAsync();

		await manager.DeleteTagCategoryAsync(manager.NoCategory);

		manager.TagCategories.ShouldContain(manager.NoCategory);
	}

	[Fact]
	public async Task CreateTagImmediatelyAsync_ShouldCreateTagAndAliases_WhenValidArgumentsProvided() {
		var dbFactory = this.CreateSqliteDbFactory();
		await this.SeedDatabaseAsync(dbFactory);
		var manager = new TagsManager(dbFactory, new TagModelFactory());
		await manager.InitializeAsync();

		var aliases = new List<ITagAliasModel> {
			new TagAliasModel { Alias = "NewAlias1", Ruby = "にゅーえいりあす" }
		};

		var result = await manager.CreateTagImmediatelyAsync(1, "NewTag", null, "NewDetail", aliases);
		result.ShouldNotBeNull();
		result.TagName.ShouldBe("NewTag");
		result.Detail.ShouldBe("NewDetail");
		manager.Tags.ShouldContain(result);
	}

	[Fact]
	public async Task CreateTagImmediatelyAsync_ShouldThrowException_WhenAliasesIsNull() {
		var dbFactory = this.CreateSqliteDbFactory();
		await this.SeedDatabaseAsync(dbFactory);
		var manager = new TagsManager(dbFactory, new TagModelFactory());
		await Should.ThrowAsync<ArgumentNullException>(() => manager.CreateTagImmediatelyAsync(1, "NewTag", null, "NewDetail", null!));
	}

	[Fact]
	public async Task UpdateTagAsync_ShouldUpdatePropertiesAliasesAndMoveCategory() {
		var dbFactory = this.CreateSqliteDbFactory();
		await this.SeedDatabaseAsync(dbFactory);
		var manager = new TagsManager(dbFactory, new TagModelFactory());
		await manager.InitializeAsync();

		var cat1 = manager.TagCategories.First(x => x.TagCategoryId == 1);
		var cat2 = manager.TagCategories.First(x => x.TagCategoryId == 2);
		var tag1 = cat1.Tags.First(x => x.TagId == 1);

		var notified = false;
		using var sub = tag1.Changed.Subscribe(_ => notified = true);

		var newAliases = new[] {
			new TagAliasModel { Alias = "別名A", Ruby = "べつめいえー" }
		};

		await manager.UpdateTagAsync(tag1, cat2, "UpdatedTag1", "あっぷでーと", "NewDetail", newAliases);

		notified.ShouldBeTrue();
		tag1.TagName.ShouldBe("UpdatedTag1");
		tag1.Ruby.ShouldBe("あっぷでーと");
		tag1.Detail.ShouldBe("NewDetail");
		tag1.TagCategory.ShouldBe(cat2);
		tag1.TagCategoryId.ShouldBe(2);
		cat1.Tags.ShouldNotContain(tag1);
		cat2.Tags.ShouldContain(tag1);
		tag1.TagAliases.Count.ShouldBe(1);
		tag1.TagAliases[0].Alias.ShouldBe("別名A");
		tag1.TagAliases[0].Romaji.ShouldBe("betsumeie-");

		await using var db = await dbFactory.CreateDbContextAsync();
		var dbTag = await db.Tags.Include(x => x.TagAliases).FirstAsync(x => x.TagId == 1);
		dbTag.TagName.ShouldBe("UpdatedTag1");
		dbTag.Ruby.ShouldBe("あっぷでーと");
		dbTag.Detail.ShouldBe("NewDetail");
		dbTag.TagCategoryId.ShouldBe(2);
		dbTag.TagAliases.Count.ShouldBe(1);
		dbTag.TagAliases.First().Alias.ShouldBe("別名A");
	}

	[Fact]
	public async Task DeleteTagAsync_ShouldDeleteImmediatelyFromDbAndMemory() {
		var dbFactory = this.CreateSqliteDbFactory();
		await this.SeedDatabaseAsync(dbFactory);
		var manager = new TagsManager(dbFactory, new TagModelFactory());
		await manager.InitializeAsync();

		var cat1 = manager.TagCategories.First(x => x.TagCategoryId == 1);
		var tag1 = manager.Tags.First(x => x.TagId == 1);

		await manager.DeleteTagAsync(tag1);

		manager.Tags.ShouldNotContain(tag1);
		cat1.Tags.ShouldNotContain(tag1);

		await using var db = await dbFactory.CreateDbContextAsync();
		var dbTag = await db.Tags.FirstOrDefaultAsync(x => x.TagId == 1);
		dbTag.ShouldBeNull();
	}

	[Fact]
	public async Task AddTagAsync_ShouldAddTagToFiles_WhenFilesDoNotHaveTag() {
		var dbFactory = this.CreateSqliteDbFactory();
		var manager = new TagsManager(dbFactory, new TagModelFactory());
		var tagMock = new Mock<ITagModel>();
		tagMock.SetupGet(x => x.TagId).Returns(99);
		tagMock.SetupGet(x => x.TagCategory).Returns(new Mock<ITagCategoryModel>().Object);
		tagMock.SetupGet(x => x.UsageCount).Returns(new ReactiveProperty<int>(0));
		var fileTags = new List<ITagModel>();
		var fileModelMock = new Mock<IMediaItemModel>();
		fileModelMock.SetupGet(x => x.Id).Returns(100);
		fileModelMock.SetupGet(x => x.Tags).Returns(fileTags);
		await using (var dbSetup = await dbFactory.CreateDbContextAsync()) {
			dbSetup.MediaItems.Add(new MediaItem { MediaType = MediaType.Image, MediaItemId = 100, FilePath = "test", DirectoryPath = "dir", Description = "", IsUnderFolderGroup = false });
			dbSetup.Tags.Add(new Tag { TagId = 99, TagName = "Tag99", Detail = "", TagCategory = null!, MediaItemTags = [], TagAliases = [] });
			await dbSetup.SaveChangesAsync();
		}
		await manager.AddTagAsync([fileModelMock.Object], tagMock.Object);
		fileTags.ShouldContain(tagMock.Object);
	}

	/// <summary>
	/// タグが null の場合、タグ付けを行わず DB も更新されないことを検証します。
	/// </summary>
	[Fact]
	public async Task AddTagAsync_ShouldDoNothing_WhenTagIsNull() {
		var dbFactory = this.CreateSqliteDbFactory();
		var manager = new TagsManager(dbFactory, new TagModelFactory());

		var fileTags = new List<ITagModel>();
		var fileModelMock = new Mock<IMediaItemModel>();
		fileModelMock.SetupGet(x => x.Tags).Returns(fileTags);

		await manager.AddTagAsync([fileModelMock.Object], null!);

		fileTags.ShouldBeEmpty();
		await using var db = await dbFactory.CreateDbContextAsync();
		(await db.MediaItemTags.CountAsync()).ShouldBe(0);
	}

	[Fact]
	public async Task AddTagAsync_ShouldDoNothing_WhenFilesAlreadyHaveTag() {
		var dbFactory = this.CreateSqliteDbFactory();
		var manager = new TagsManager(dbFactory, new TagModelFactory());
		var tagMock = new Mock<ITagModel>();
		tagMock.SetupGet(x => x.TagId).Returns(99);
		tagMock.SetupGet(x => x.TagCategory).Returns(new Mock<ITagCategoryModel>().Object);
		tagMock.SetupGet(x => x.UsageCount).Returns(new ReactiveProperty<int>(0));
		var fileTags = new List<ITagModel> { tagMock.Object };
		var fileModelMock = new Mock<IMediaItemModel>();
		fileModelMock.SetupGet(x => x.Id).Returns(100);
		fileModelMock.SetupGet(x => x.Tags).Returns(fileTags);
		await manager.AddTagAsync([fileModelMock.Object], tagMock.Object);
		await using var db = await dbFactory.CreateDbContextAsync();
		var rel = await db.MediaItemTags.FirstOrDefaultAsync(x => x.MediaItemId == 100 && x.TagId == 99);
		rel.ShouldBeNull();
	}

	[Fact]
	public async Task RemoveTagAsync_ShouldRemoveTagFromFiles_WhenFilesHaveTag() {
		var dbFactory = this.CreateSqliteDbFactory();
		var manager = new TagsManager(dbFactory, new TagModelFactory());
		var tagMock = new Mock<ITagModel>();
		tagMock.SetupGet(x => x.TagId).Returns(99);
		tagMock.SetupGet(x => x.TagCategory).Returns(new Mock<ITagCategoryModel>().Object);
		tagMock.SetupGet(x => x.UsageCount).Returns(new ReactiveProperty<int>(1));
		var fileTags = new List<ITagModel> { tagMock.Object };
		var fileModelMock = new Mock<IMediaItemModel>();
		fileModelMock.SetupGet(x => x.Id).Returns(100);
		fileModelMock.SetupGet(x => x.Tags).Returns(fileTags);
		await using (var dbSetup = await dbFactory.CreateDbContextAsync()) {
			dbSetup.MediaItems.Add(new MediaItem { MediaType = MediaType.Image, MediaItemId = 100, FilePath = "test2", DirectoryPath = "dir", Description = "", IsUnderFolderGroup = false });
			dbSetup.Tags.Add(new Tag { TagId = 99, TagName = "Tag99", Detail = "", TagCategory = null!, MediaItemTags = [], TagAliases = [] });
			dbSetup.MediaItemTags.Add(new MediaItemTag { MediaItemId = 100, TagId = 99 });
			await dbSetup.SaveChangesAsync();
		}
		await manager.RemoveTagAsync([fileModelMock.Object], 99);
		fileTags.ShouldBeEmpty();
	}

	/// <summary>
	/// 対象ファイルが指定されたタグを保持していない場合、何も削除されず DB も更新されないことを検証します。
	/// </summary>
	[Fact]
	public async Task RemoveTagAsync_ShouldDoNothing_WhenFilesDoNotHaveTag() {
		var dbFactory = this.CreateSqliteDbFactory();
		var manager = new TagsManager(dbFactory, new TagModelFactory());
		var fileTags = new List<ITagModel>();
		var fileModelMock = new Mock<IMediaItemModel>();
		fileModelMock.SetupGet(x => x.Id).Returns(100);
		fileModelMock.SetupGet(x => x.Tags).Returns(fileTags);

		await manager.RemoveTagAsync([fileModelMock.Object], 99);

		fileTags.ShouldBeEmpty();
		await using var db = await dbFactory.CreateDbContextAsync();
		(await db.MediaItemTags.CountAsync()).ShouldBe(0);
	}

	[Fact]
	public async Task InitializeAsync_ShouldLoadTagsAndCategoriesFromDatabase() {
		var dbFactory = this.CreateSqliteDbFactory();
		await this.SeedDatabaseAsync(dbFactory);
		var manager = new TagsManager(dbFactory, new TagModelFactory());
		await manager.InitializeAsync();
		manager.TagCategories.Count.ShouldBe(3); // +1 Virtual Category for No Category tags
		manager.Tags.Count.ShouldBe(2);
	}

	/// <summary>
	/// 初期化時に使用件数の集計・使用回数降順（同件数時はタグID昇順）並び替え・別名読み込み・カテゴリー順序が正しく行われることを検証します。
	/// </summary>
	[Fact]
	public async Task InitializeAsync_ShouldAggregateUsageCountsSortTagsAndLoadAliasesWithoutTrackingMediaItemTags() {
		var dbFactory = this.CreateSqliteDbFactory();
		await using (var db = await dbFactory.CreateDbContextAsync()) {
			db.TagCategories.AddRange(
				new TagCategory { TagCategoryId = 2, TagCategoryName = "Cat2", Detail = "", Tags = [] },
				new TagCategory { TagCategoryId = 1, TagCategoryName = "Cat1", Detail = "", Tags = [] });

			db.Tags.AddRange(
				new Tag { TagId = 10, TagCategoryId = 1, TagName = "Cat1_Zero", Detail = "", TagCategory = null!, MediaItemTags = [], TagAliases = [] },
				new Tag { TagId = 11, TagCategoryId = 1, TagName = "Cat1_Two_HighId", Detail = "", TagCategory = null!, MediaItemTags = [], TagAliases = [new TagAlias { TagId = 11, TagAliasId = 0, Alias = "Alias11" }] },
				new Tag { TagId = 12, TagCategoryId = 1, TagName = "Cat1_Three", Detail = "", TagCategory = null!, MediaItemTags = [], TagAliases = [new TagAlias { TagId = 12, TagAliasId = 0, Alias = "Alias12A" }, new TagAlias { TagId = 12, TagAliasId = 1, Alias = "Alias12B" }] },
				new Tag { TagId = 9, TagCategoryId = 1, TagName = "Cat1_Two_LowId", Detail = "", TagCategory = null!, MediaItemTags = [], TagAliases = [] },
				new Tag { TagId = 20, TagCategoryId = null, TagName = "NoCat_Zero", Detail = "", TagCategory = null!, MediaItemTags = [], TagAliases = [] },
				new Tag { TagId = 21, TagCategoryId = null, TagName = "NoCat_Two", Detail = "", TagCategory = null!, MediaItemTags = [], TagAliases = [new TagAlias { TagId = 21, TagAliasId = 0, Alias = "Alias21" }] },
				new Tag { TagId = 22, TagCategoryId = null, TagName = "NoCat_One", Detail = "", TagCategory = null!, MediaItemTags = [], TagAliases = [] });

			db.MediaItems.AddRange(
				new MediaItem { MediaType = MediaType.Image, MediaItemId = 1, FilePath = "f1", DirectoryPath = "d", Description = "", IsUnderFolderGroup = false },
				new MediaItem { MediaType = MediaType.Image, MediaItemId = 2, FilePath = "f2", DirectoryPath = "d", Description = "", IsUnderFolderGroup = false },
				new MediaItem { MediaType = MediaType.Image, MediaItemId = 3, FilePath = "f3", DirectoryPath = "d", Description = "", IsUnderFolderGroup = false });

			db.MediaItemTags.AddRange(
				new MediaItemTag { MediaItemId = 1, TagId = 12 },
				new MediaItemTag { MediaItemId = 2, TagId = 12 },
				new MediaItemTag { MediaItemId = 3, TagId = 12 },
				new MediaItemTag { MediaItemId = 1, TagId = 11 },
				new MediaItemTag { MediaItemId = 2, TagId = 11 },
				new MediaItemTag { MediaItemId = 1, TagId = 9 },
				new MediaItemTag { MediaItemId = 2, TagId = 9 },
				new MediaItemTag { MediaItemId = 1, TagId = 21 },
				new MediaItemTag { MediaItemId = 2, TagId = 21 },
				new MediaItemTag { MediaItemId = 1, TagId = 22 });

			await db.SaveChangesAsync();
		}

		using var manager = new TagsManager(dbFactory, new TagModelFactory());
		await manager.InitializeAsync();

		// カテゴリー順（未分類 → TagCategoryId 昇順）
		manager.TagCategories.Select(c => c.TagCategoryId).ShouldBe([null, 1, 2]);

		// 未分類カテゴリーの並び順と使用件数
		manager.NoCategory.Tags.Select(t => t.TagId).ShouldBe([21, 22, 20]);
		manager.NoCategory.Tags.Select(t => t.UsageCount.Value).ShouldBe([2, 1, 0]);

		// Category1 の並び順（使用回数降順・同件数時は TagId 昇順）と使用件数
		var cat1 = manager.TagCategories.First(c => c.TagCategoryId == 1);
		cat1.Tags.Select(t => t.TagId).ShouldBe([12, 9, 11, 10]);
		cat1.Tags.Select(t => t.UsageCount.Value).ShouldBe([3, 2, 2, 0]);

		// Category2 は空
		var cat2 = manager.TagCategories.First(c => c.TagCategoryId == 2);
		cat2.Tags.ShouldBeEmpty();

		// TagAliases（0件・複数件）の確認
		manager.Tags.First(t => t.TagId == 10).TagAliases.ShouldBeEmpty();
		manager.Tags.First(t => t.TagId == 11).TagAliases.Select(a => a.Alias).ShouldBe(["Alias11"]);
		manager.Tags.First(t => t.TagId == 12).TagAliases.Select(a => a.Alias).ShouldBe(["Alias12A", "Alias12B"], ignoreOrder: true);
		manager.Tags.First(t => t.TagId == 21).TagAliases.Select(a => a.Alias).ShouldBe(["Alias21"]);
	}

	[Fact]
	public async Task UpdateTagAsync_ShouldEmitTagChanged() {
		var dbFactory = this.CreateSqliteDbFactory();
		await this.SeedDatabaseAsync(dbFactory);
		using var manager = new TagsManager(dbFactory, new TagModelFactory());
		await manager.InitializeAsync();

		var tag1 = manager.Tags.First(x => x.TagId == 1);
		var cat1 = manager.TagCategories.First(x => x.TagCategoryId == 1);
		ITagModel? notifiedTag = null;
		using var sub = manager.TagChanged.Subscribe(t => notifiedTag = t);

		await manager.UpdateTagAsync(tag1, cat1, "RenamedTag", null, "", []);

		notifiedTag.ShouldBe(tag1);
		notifiedTag!.TagName.ShouldBe("RenamedTag");
	}

	[Fact]
	public async Task DeleteTagCategoryAsync_ShouldEmitTagChangedForMovedTags() {
		var dbFactory = this.CreateSqliteDbFactory();
		await this.SeedDatabaseAsync(dbFactory);
		using var manager = new TagsManager(dbFactory, new TagModelFactory());
		await manager.InitializeAsync();

		var cat1 = manager.TagCategories.First(x => x.TagCategoryId == 1);
		var movedIds = new List<int>();
		using var sub = manager.TagChanged.Subscribe(t => movedIds.Add(t.TagId));

		await manager.DeleteTagCategoryAsync(cat1);

		movedIds.ShouldBe([1, 2], ignoreOrder: true);
	}
}