using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediaDeck.Composition.Interfaces.Files;
using MediaDeck.Composition.Interfaces.FileTypes.Models;
using MediaDeck.Composition.Interfaces.Tags;
using MediaDeck.Core.Models.Tags;
using MediaDeck.Database;
using MediaDeck.Database.Tables;
using Microsoft.EntityFrameworkCore;
using Moq;
using ObservableCollections;
using R3;
using Shouldly;
using Xunit;

namespace MediaDeck.Core.Tests.Models.Tags;

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
		db.Tags.Add(new Tag { TagId = 1, TagCategoryId = 1, TagName = "Tag1", Detail = "Detail1", TagCategory = null! });
		db.Tags.Add(new Tag { TagId = 2, TagCategoryId = 1, TagName = "Tag2", Detail = "Detail2", TagCategory = null! });
		db.TagAliases.Add(new TagAlias { TagId = 1, TagAliasId = 1, Alias = "Alias1" });
		await db.SaveChangesAsync();
	}

	private void SetupFactoryMock(Mock<ITagModelFactory> mock) {
		mock.Setup(f => f.CreateCategory(It.IsAny<TagCategory>())).Returns((TagCategory c) => {
			var m = new Mock<ITagCategoryModel>();
			m.SetupGet(x => x.TagCategoryId).Returns(c.TagCategoryId);
			// 簡易的に、カテゴリ内のタグをモデルに変換して返す（実際のLoadの挙動を模倣）
			var tagModels = c.Tags.Select(t => {
				var tm = new Mock<ITagModel>();
				tm.SetupGet(x => x.TagId).Returns(t.TagId);
				tm.SetupGet(x => x.TagName).Returns(t.TagName);
				tm.SetupGet(x => x.UsageCount).Returns(new ReactiveProperty<int>(t.MediaFileTags?.Count ?? 0));
				return tm.Object;
			}).ToList();
			m.SetupGet(x => x.Tags).Returns(new ObservableList<ITagModel>(tagModels));
			return m.Object;
		});
		mock.Setup(f => f.Create(It.IsAny<Tag>(), It.IsAny<ITagCategoryModel>())).Returns((Tag t, ITagCategoryModel? c) => {
			var m = new Mock<ITagModel>();
			m.SetupGet(x => x.TagId).Returns(t.TagId);
			m.SetupGet(x => x.TagName).Returns(t.TagName);
			m.SetupGet(x => x.UsageCount).Returns(new ReactiveProperty<int>(t.MediaFileTags?.Count ?? 0));
			return m.Object;
		});
		mock.Setup(f => f.Create(It.IsAny<Tag>(), It.IsAny<ITagCategoryModel>())).Returns((Tag t, ITagCategoryModel c) => {
			var m = new Mock<ITagModel>();
			m.SetupGet(x => x.TagId).Returns(t.TagId);
			m.SetupGet(x => x.TagName).Returns(t.TagName);
			m.SetupGet(x => x.UsageCount).Returns(new ReactiveProperty<int>(t.MediaFileTags?.Count ?? 0));
			return m.Object;
		});
	}

	/// <summary>
	/// FindTagByNameAsyncが、名前が一致するタグが存在する場合にそのタグを返すことを検証します。
	/// </summary>
	[Fact]
	public async Task FindTagByNameAsync_ShouldReturnTag_WhenTagExists() {
		var dbFactory = this.CreateInMemoryDbFactory(nameof(this.FindTagByNameAsync_ShouldReturnTag_WhenTagExists));
		var tagModelFactoryMock = new Mock<ITagModelFactory>();
		var manager = new TagsManager(dbFactory, tagModelFactoryMock.Object);
		var tagMock = new Mock<ITagModel>();
		tagMock.SetupGet(x => x.TagName).Returns("ExistingTag");
		tagMock.SetupGet(x => x.UsageCount).Returns(new ReactiveProperty<int>(0));
		manager.Tags.Add(tagMock.Object);
		var result = await manager.FindTagByNameAsync("ExistingTag");
		result.ShouldNotBeNull();
		result.TagName.ShouldBe("ExistingTag");
	}

	/// <summary>
	/// FindTagByNameAsyncが、名前が一致するタグが存在しない場合にnullを返すことを検証します。
	/// </summary>
	[Fact]
	public async Task FindTagByNameAsync_ShouldReturnNull_WhenTagDoesNotExist() {
		var dbFactory = this.CreateInMemoryDbFactory(nameof(this.FindTagByNameAsync_ShouldReturnNull_WhenTagDoesNotExist));
		var tagModelFactoryMock = new Mock<ITagModelFactory>();
		var manager = new TagsManager(dbFactory, tagModelFactoryMock.Object);
		var result = await manager.FindTagByNameAsync("NonExistingTag");
		result.ShouldBeNull();
	}

	/// <summary>
	/// CreateTagAsyncが新しいタグとエイリアスを正常に作成し、データベースに保存することを検証します。
	/// </summary>
	[Fact]
	public async Task CreateTagAsync_ShouldCreateTagAndAliases_WhenValidArgumentsProvided() {
		var dbFactory = this.CreateInMemoryDbFactory(nameof(this.CreateTagAsync_ShouldCreateTagAndAliases_WhenValidArgumentsProvided));
		await this.SeedDatabaseAsync(dbFactory);
		var tagModelFactoryMock = new Mock<ITagModelFactory>();
		this.SetupFactoryMock(tagModelFactoryMock);
		var manager = new TagsManager(dbFactory, tagModelFactoryMock.Object);
		var aliases = new List<ITagAliasModel>();
		var aliasMock = new Mock<ITagAliasModel>();
		aliasMock.SetupGet(x => x.Alias).Returns("NewAlias1");
		aliases.Add(aliasMock.Object);

		var result = await manager.CreateTagAsync(1, "NewTag", "NewDetail", aliases);
		result.ShouldNotBeNull();
		result.TagName.ShouldBe("NewTag");
	}

	/// <summary>
	/// CreateTagAsyncが、エイリアスリストにnullが渡された場合に例外を投げることを検証します。
	/// </summary>
	[Fact]
	public async Task CreateTagAsync_ShouldThrowException_WhenAliasesIsNull() {
		var dbFactory = this.CreateInMemoryDbFactory(nameof(this.CreateTagAsync_ShouldThrowException_WhenAliasesIsNull));
		var tagModelFactoryMock = new Mock<ITagModelFactory>();
		var manager = new TagsManager(dbFactory, tagModelFactoryMock.Object);
		await Should.ThrowAsync<Exception>(() => manager.CreateTagAsync(1, "NewTag", "NewDetail", null!));
	}

	/// <summary>
	/// AddTagAsyncが、指定されたタグを持たないファイルに対してタグを追加することを検証します。
	/// </summary>
	[Fact]
	public async Task AddTagAsync_ShouldAddTagToFiles_WhenFilesDoNotHaveTag() {
		var dbFactory = this.CreateInMemoryDbFactory(nameof(this.AddTagAsync_ShouldAddTagToFiles_WhenFilesDoNotHaveTag));
		var tagModelFactoryMock = new Mock<ITagModelFactory>();
		var manager = new TagsManager(dbFactory, tagModelFactoryMock.Object);
		var tagMock = new Mock<ITagModel>();
		tagMock.SetupGet(x => x.TagId).Returns(99);
		tagMock.SetupGet(x => x.UsageCount).Returns(new ReactiveProperty<int>(0));
		var fileTags = new List<ITagModel>();
		var fileModelMock = new Mock<IFileModel>();
		fileModelMock.SetupGet(x => x.Id).Returns(100);
		fileModelMock.SetupGet(x => x.Tags).Returns(fileTags);
		await using (var dbSetup = await dbFactory.CreateDbContextAsync()) {
			dbSetup.MediaFiles.Add(new MediaFile { MediaFileId = 100, FilePath = "test", DirectoryPath = "dir", Description = "" });
			dbSetup.Tags.Add(new Tag { TagId = 99, TagName = "Tag99", Detail = "", TagCategory = null! });
			await dbSetup.SaveChangesAsync();
		}
		await manager.AddTagAsync([fileModelMock.Object], tagMock.Object);
		fileTags.ShouldContain(tagMock.Object);
	}

	/// <summary>
	/// AddTagAsyncが、引数のタグにnullが渡された場合に何もせずリターンすることを検証します（異常系：ガード節の確認）。
	/// </summary>
	[Fact]
	public async Task AddTagAsync_ShouldDoNothing_WhenTagIsNull() {
		var dbFactory = this.CreateInMemoryDbFactory(nameof(this.AddTagAsync_ShouldDoNothing_WhenTagIsNull));
		var tagModelFactoryMock = new Mock<ITagModelFactory>();
		var manager = new TagsManager(dbFactory, tagModelFactoryMock.Object);

		var fileModelMock = new Mock<IFileModel>();
		fileModelMock.SetupGet(x => x.Tags).Returns([]);

		await manager.AddTagAsync([fileModelMock.Object], null!);

		// 例外が発生せずに終了することを確認
	}

	/// <summary>
	/// AddTagAsyncが、既に指定されたタグを持っているファイルに対しては何もしないことを検証します。
	/// </summary>
	[Fact]
	public async Task AddTagAsync_ShouldDoNothing_WhenFilesAlreadyHaveTag() {
		var dbFactory = this.CreateInMemoryDbFactory(nameof(this.AddTagAsync_ShouldDoNothing_WhenFilesAlreadyHaveTag));
		var tagModelFactoryMock = new Mock<ITagModelFactory>();
		var manager = new TagsManager(dbFactory, tagModelFactoryMock.Object);
		var tagMock = new Mock<ITagModel>();
		tagMock.SetupGet(x => x.TagId).Returns(99);
		tagMock.SetupGet(x => x.UsageCount).Returns(new ReactiveProperty<int>(0));
		var fileTags = new List<ITagModel> { tagMock.Object };
		var fileModelMock = new Mock<IFileModel>();
		fileModelMock.SetupGet(x => x.Id).Returns(100);
		fileModelMock.SetupGet(x => x.Tags).Returns(fileTags);
		await manager.AddTagAsync([fileModelMock.Object], tagMock.Object);
		await using var db = await dbFactory.CreateDbContextAsync();
		var rel = await db.MediaFileTags.FirstOrDefaultAsync(x => x.MediaFileId == 100 && x.TagId == 99);
		rel.ShouldBeNull();
	}

	/// <summary>
	/// RemoveTagAsyncが、指定されたタグを持つファイルからタグを削除することを検証します。
	/// </summary>
	[Fact]
	public async Task RemoveTagAsync_ShouldRemoveTagFromFiles_WhenFilesHaveTag() {
		var dbFactory = this.CreateInMemoryDbFactory(nameof(this.RemoveTagAsync_ShouldRemoveTagFromFiles_WhenFilesHaveTag));
		var tagModelFactoryMock = new Mock<ITagModelFactory>();
		var manager = new TagsManager(dbFactory, tagModelFactoryMock.Object);
		var tagMock = new Mock<ITagModel>();
		tagMock.SetupGet(x => x.TagId).Returns(99);
		tagMock.SetupGet(x => x.UsageCount).Returns(new ReactiveProperty<int>(1));
		var fileTags = new List<ITagModel> { tagMock.Object };
		var fileModelMock = new Mock<IFileModel>();
		fileModelMock.SetupGet(x => x.Id).Returns(100);
		fileModelMock.SetupGet(x => x.Tags).Returns(fileTags);
		await using (var dbSetup = await dbFactory.CreateDbContextAsync()) {
			dbSetup.MediaFiles.Add(new MediaFile { MediaFileId = 100, FilePath = "test2", DirectoryPath = "dir", Description = "" });
			dbSetup.Tags.Add(new Tag { TagId = 99, TagName = "Tag99", Detail = "", TagCategory = null! });
			dbSetup.MediaFileTags.Add(new MediaFileTag { MediaFileId = 100, TagId = 99 });
			await dbSetup.SaveChangesAsync();
		}
		await manager.RemoveTagAsync([fileModelMock.Object], 99);
		fileTags.ShouldBeEmpty();
	}

	/// <summary>
	/// RemoveTagAsyncが、指定されたタグを持たないファイルに対しては何もしないことを検証します。
	/// </summary>
	[Fact]
	public async Task RemoveTagAsync_ShouldDoNothing_WhenFilesDoNotHaveTag() {
		var dbFactory = this.CreateInMemoryDbFactory(nameof(this.RemoveTagAsync_ShouldDoNothing_WhenFilesDoNotHaveTag));
		var tagModelFactoryMock = new Mock<ITagModelFactory>();
		var manager = new TagsManager(dbFactory, tagModelFactoryMock.Object);
		var fileModelMock = new Mock<IFileModel>();
		fileModelMock.SetupGet(x => x.Id).Returns(100);
		fileModelMock.SetupGet(x => x.Tags).Returns(new List<ITagModel>());
		await manager.RemoveTagAsync([fileModelMock.Object], 99);
	}

	/// <summary>
	/// UpdateTagAsyncが存在しないIDを更新しようとした場合に例外（FirstAsyncによるInvalidOperationException）を投げることを検証します。
	/// </summary>
	[Fact]
	public async Task UpdateTagAsync_ShouldThrowInvalidOperationException_WhenTagDoesNotExist() {
		var dbFactory = this.CreateInMemoryDbFactory(nameof(this.UpdateTagAsync_ShouldThrowInvalidOperationException_WhenTagDoesNotExist));
		var tagModelFactoryMock = new Mock<ITagModelFactory>();
		var manager = new TagsManager(dbFactory, tagModelFactoryMock.Object);

		await Should.ThrowAsync<InvalidOperationException>(() => manager.UpdateTagAsync(999, 1, "Name", "Detail", []));
	}

	/// <summary>
	/// UpdateTagAsyncが既存タグの情報とエイリアスを正しく更新することを検証します。
	/// </summary>
	[Fact]
	public async Task UpdateTagAsync_ShouldUpdateTagAndAliases_WhenTagExists() {
		var dbFactory = this.CreateInMemoryDbFactory(nameof(this.UpdateTagAsync_ShouldUpdateTagAndAliases_WhenTagExists));
		await this.SeedDatabaseAsync(dbFactory);
		var tagModelFactoryMock = new Mock<ITagModelFactory>();
		this.SetupFactoryMock(tagModelFactoryMock);
		var manager = new TagsManager(dbFactory, tagModelFactoryMock.Object);
		var newAliases = new List<ITagAliasModel>();
		var aliasMock = new Mock<ITagAliasModel>();
		aliasMock.SetupGet(x => x.Alias).Returns("UpdatedAlias1");
		aliasMock.SetupGet(x => x.Ruby).Returns("UpdatedRuby1");
		newAliases.Add(aliasMock.Object);
		await manager.UpdateTagAsync(1, 2, "UpdatedTag1", "UpdatedDetail1", newAliases);
		await using var db = await dbFactory.CreateDbContextAsync();
		var dbTag = await db.Tags.Include(x => x.TagAliases).FirstAsync(x => x.TagId == 1);
		dbTag.TagCategoryId.ShouldBe(2);
		dbTag.TagName.ShouldBe("UpdatedTag1");
		dbTag.Detail.ShouldBe("UpdatedDetail1");
		dbTag.TagAliases.Count.ShouldBe(1);
		dbTag.TagAliases.First().Alias.ShouldBe("UpdatedAlias1");
		dbTag.TagAliases.First().Ruby.ShouldBe("UpdatedRuby1");
	}

	/// <summary>
	/// UpdateTagCategoryAsyncが既存のカテゴリを更新することを検証します。
	/// </summary>
	[Fact]
	public async Task UpdateTagCategoryAsync_ShouldUpdateCategory_WhenCategoryExists() {
		var dbFactory = this.CreateInMemoryDbFactory(nameof(this.UpdateTagCategoryAsync_ShouldUpdateCategory_WhenCategoryExists));
		await this.SeedDatabaseAsync(dbFactory);
		var tagModelFactoryMock = new Mock<ITagModelFactory>();
		this.SetupFactoryMock(tagModelFactoryMock);
		var manager = new TagsManager(dbFactory, tagModelFactoryMock.Object);
		await manager.UpdateTagCategoryAsync(1, "UpdatedCategoryName", "UpdatedCatDetail");
		await using var db = await dbFactory.CreateDbContextAsync();
		var dbCat = await db.TagCategories.FirstAsync(x => x.TagCategoryId == 1);
		dbCat.TagCategoryName.ShouldBe("UpdatedCategoryName");
	}

	/// <summary>
	/// UpdateTagCategoryAsyncが、存在しないIDを指定された場合に新規カテゴリを作成することを検証します。
	/// </summary>
	[Fact]
	public async Task UpdateTagCategoryAsync_ShouldCreateCategory_WhenCategoryDoesNotExist() {
		var dbFactory = this.CreateInMemoryDbFactory(nameof(this.UpdateTagCategoryAsync_ShouldCreateCategory_WhenCategoryDoesNotExist));
		var tagModelFactoryMock = new Mock<ITagModelFactory>();
		this.SetupFactoryMock(tagModelFactoryMock);
		var manager = new TagsManager(dbFactory, tagModelFactoryMock.Object);
		await manager.UpdateTagCategoryAsync(3, "NewCategoryName", "NewCatDetail");
		await using var db = await dbFactory.CreateDbContextAsync();
		var dbCat = await db.TagCategories.FirstOrDefaultAsync(x => x.TagCategoryId == 3);
		dbCat.ShouldNotBeNull();
		dbCat.TagCategoryName.ShouldBe("NewCategoryName");
	}

	/// <summary>
	/// Loadがデータベースからカテゴリとタグを読み込み、ファクトリを介して正しくモデルに変換することを検証します。
	/// </summary>
	[Fact]
	public async Task Load_ShouldLoadTagsAndCategoriesFromDatabase() {
		var dbFactory = this.CreateInMemoryDbFactory(nameof(this.Load_ShouldLoadTagsAndCategoriesFromDatabase));
		await this.SeedDatabaseAsync(dbFactory);
		var tagModelFactoryMock = new Mock<ITagModelFactory>();
		this.SetupFactoryMock(tagModelFactoryMock);
		var manager = new TagsManager(dbFactory, tagModelFactoryMock.Object);
		await manager.Load();
		manager.TagCategories.Count.ShouldBe(2);
		manager.Tags.Count.ShouldBe(2);
	}
}