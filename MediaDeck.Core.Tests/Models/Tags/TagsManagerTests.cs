using MediaDeck.Composition.Database;
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
public class TagsManagerTests {
	private IDbContextFactory<MediaDeckDbContext> CreateInMemoryDbFactory(string dbName) {
		var options = new DbContextOptionsBuilder<MediaDeckDbContext>()
			.UseInMemoryDatabase(databaseName: dbName)
			.ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
			.Options;

		var factoryMock = new Mock<IDbContextFactory<MediaDeckDbContext>>();
		factoryMock.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(() => new MediaDeckDbContext(options));

		return factoryMock.Object;
	}

	private async Task SeedDatabaseAsync(IDbContextFactory<MediaDeckDbContext> dbFactory) {
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
		var dbName = nameof(this.FindTagByNameAsync_ShouldReturnTag_WhenTagExists);
		var dbFactory = this.CreateInMemoryDbFactory(dbName);
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
		var dbFactory = this.CreateInMemoryDbFactory(nameof(this.FindTagByNameAsync_ShouldReturnNull_WhenTagDoesNotExist));
		var manager = new TagsManager(dbFactory, new TagModelFactory());
		var result = await manager.FindTagByNameAsync("NonExistingTag");
		result.ShouldBeNull();
	}

	[Fact]
	public async Task CreateTagCategoryAsync_ShouldCreateAndPersistCategoryImmediately() {
		var dbFactory = this.CreateInMemoryDbFactory(nameof(this.CreateTagCategoryAsync_ShouldCreateAndPersistCategoryImmediately));
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
		var dbFactory = this.CreateInMemoryDbFactory(nameof(this.UpdateTagCategoryAsync_ShouldUpdateAndPersistCategory));
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
		var dbFactory = this.CreateInMemoryDbFactory(nameof(this.UpdateTagCategoryAsync_ShouldDoNothing_ForNoCategory));
		await this.SeedDatabaseAsync(dbFactory);
		var manager = new TagsManager(dbFactory, new TagModelFactory());
		await manager.InitializeAsync();

		var originalName = manager.NoCategory.TagCategoryName;
		await manager.UpdateTagCategoryAsync(manager.NoCategory, "Modified", "ModifiedDetail");

		manager.NoCategory.TagCategoryName.ShouldBe(originalName);
	}

	[Fact]
	public async Task DeleteTagCategoryAsync_ShouldDeleteImmediatelyAndMoveTagsToNoCategory() {
		var dbFactory = this.CreateInMemoryDbFactory(nameof(this.DeleteTagCategoryAsync_ShouldDeleteImmediatelyAndMoveTagsToNoCategory));
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
		var dbFactory = this.CreateInMemoryDbFactory(nameof(this.DeleteTagCategoryAsync_ShouldDoNothing_ForNoCategory));
		await this.SeedDatabaseAsync(dbFactory);
		var manager = new TagsManager(dbFactory, new TagModelFactory());
		await manager.InitializeAsync();

		await manager.DeleteTagCategoryAsync(manager.NoCategory);

		manager.TagCategories.ShouldContain(manager.NoCategory);
	}

	[Fact]
	public async Task CreateTagImmediatelyAsync_ShouldCreateTagAndAliases_WhenValidArgumentsProvided() {
		var dbFactory = this.CreateInMemoryDbFactory(nameof(this.CreateTagImmediatelyAsync_ShouldCreateTagAndAliases_WhenValidArgumentsProvided));
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
		var dbFactory = this.CreateInMemoryDbFactory(nameof(this.CreateTagImmediatelyAsync_ShouldThrowException_WhenAliasesIsNull));
		var manager = new TagsManager(dbFactory, new TagModelFactory());
		await Should.ThrowAsync<Exception>(() => manager.CreateTagImmediatelyAsync(1, "NewTag", null, "NewDetail", null!));
	}

	[Fact]
	public async Task UpdateTagAsync_ShouldUpdatePropertiesAliasesAndMoveCategory() {
		var dbFactory = this.CreateInMemoryDbFactory(nameof(this.UpdateTagAsync_ShouldUpdatePropertiesAliasesAndMoveCategory));
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
		var dbFactory = this.CreateInMemoryDbFactory(nameof(this.DeleteTagAsync_ShouldDeleteImmediatelyFromDbAndMemory));
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
		var dbFactory = this.CreateInMemoryDbFactory(nameof(this.AddTagAsync_ShouldAddTagToFiles_WhenFilesDoNotHaveTag));
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

	[Fact]
	public async Task AddTagAsync_ShouldDoNothing_WhenTagIsNull() {
		var dbFactory = this.CreateInMemoryDbFactory(nameof(this.AddTagAsync_ShouldDoNothing_WhenTagIsNull));
		var manager = new TagsManager(dbFactory, new TagModelFactory());

		var fileModelMock = new Mock<IMediaItemModel>();
		fileModelMock.SetupGet(x => x.Tags).Returns([]);

		await manager.AddTagAsync([fileModelMock.Object], null!);
	}

	[Fact]
	public async Task AddTagAsync_ShouldDoNothing_WhenFilesAlreadyHaveTag() {
		var dbFactory = this.CreateInMemoryDbFactory(nameof(this.AddTagAsync_ShouldDoNothing_WhenFilesAlreadyHaveTag));
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
		var dbFactory = this.CreateInMemoryDbFactory(nameof(this.RemoveTagAsync_ShouldRemoveTagFromFiles_WhenFilesHaveTag));
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

	[Fact]
	public async Task RemoveTagAsync_ShouldDoNothing_WhenFilesDoNotHaveTag() {
		var dbFactory = this.CreateInMemoryDbFactory(nameof(this.RemoveTagAsync_ShouldDoNothing_WhenFilesDoNotHaveTag));
		var manager = new TagsManager(dbFactory, new TagModelFactory());
		var fileModelMock = new Mock<IMediaItemModel>();
		fileModelMock.SetupGet(x => x.Id).Returns(100);
		fileModelMock.SetupGet(x => x.Tags).Returns(new List<ITagModel>());
		await manager.RemoveTagAsync([fileModelMock.Object], 99);
	}

	[Fact]
	public async Task InitializeAsync_ShouldLoadTagsAndCategoriesFromDatabase() {
		var dbFactory = this.CreateInMemoryDbFactory(nameof(this.InitializeAsync_ShouldLoadTagsAndCategoriesFromDatabase));
		await this.SeedDatabaseAsync(dbFactory);
		var manager = new TagsManager(dbFactory, new TagModelFactory());
		await manager.InitializeAsync();
		manager.TagCategories.Count.ShouldBe(3); // +1 Virtual Category for No Category tags
		manager.Tags.Count.ShouldBe(2);
	}

	[Fact]
	public async Task UpdateTagAsync_ShouldEmitTagChanged() {
		var dbFactory = this.CreateInMemoryDbFactory(nameof(this.UpdateTagAsync_ShouldEmitTagChanged));
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
		var dbFactory = this.CreateInMemoryDbFactory(nameof(this.DeleteTagCategoryAsync_ShouldEmitTagChangedForMovedTags));
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