using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediaDeck.Composition.Interfaces.Files;
using MediaDeck.Composition.Interfaces.FileTypes.Models;
using MediaDeck.Core.Models.FileDetailManagers;
using MediaDeck.Database;
using MediaDeck.Database.Tables;
using Microsoft.EntityFrameworkCore;
using Moq;
using Shouldly;
using Xunit;

namespace MediaDeck.Core.Tests.Models.FileDetailManagers;

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
		db.Tags.Add(new Tag { TagId = 1, TagCategoryId = 1, TagName = "Tag1", Detail = "Detail1", TagCategory = null! });
		db.Tags.Add(new Tag { TagId = 2, TagCategoryId = 1, TagName = "Tag2", Detail = "Detail2", TagCategory = null! });
		db.TagAliases.Add(new TagAlias { TagId = 1, TagAliasId = 1, Alias = "Alias1" });
		await db.SaveChangesAsync();
	}

	/// <summary>
	/// FindTagByNameAsyncメソッドが既存のタグを正しく返すことを検証します。
	/// </summary>
	[Fact]
	public async Task FindTagByNameAsync_ShouldReturnTag_WhenTagExists() {
		// Arrange
		var dbFactory = this.CreateInMemoryDbFactory(nameof(this.FindTagByNameAsync_ShouldReturnTag_WhenTagExists));
		var tagModelFactoryMock = new Mock<ITagModelFactory>();
		var manager = new TagsManager(dbFactory, tagModelFactoryMock.Object);

		var tagMock = new Mock<ITagModel>();
		tagMock.SetupGet(x => x.TagName).Returns("ExistingTag");
		manager.Tags.Add(tagMock.Object);

		// Act
		var result = await manager.FindTagByNameAsync("ExistingTag");

		// Assert
		result.ShouldNotBeNull();
		result.TagName.ShouldBe("ExistingTag");
	}

	/// <summary>
	/// FindTagByNameAsyncメソッドが存在しないタグ名の場合にnullを返すことを検証します。
	/// </summary>
	[Fact]
	public async Task FindTagByNameAsync_ShouldReturnNull_WhenTagDoesNotExist() {
		// Arrange
		var dbFactory = this.CreateInMemoryDbFactory(nameof(this.FindTagByNameAsync_ShouldReturnNull_WhenTagDoesNotExist));
		var tagModelFactoryMock = new Mock<ITagModelFactory>();
		var manager = new TagsManager(dbFactory, tagModelFactoryMock.Object);

		var tagMock = new Mock<ITagModel>();
		tagMock.SetupGet(x => x.TagName).Returns("ExistingTag");
		manager.Tags.Add(tagMock.Object);

		// Act
		var result = await manager.FindTagByNameAsync("NonExistingTag");

		// Assert
		result.ShouldBeNull();
	}

	/// <summary>
	/// CreateTagAsyncメソッドが正常にタグとエイリアスをデータベースに保存し、ITagModelを返すことを検証します。
	/// </summary>
	[Fact]
	public async Task CreateTagAsync_ShouldCreateTagAndAliases_WhenValidArgumentsProvided() {
		// Arrange
		var dbFactory = this.CreateInMemoryDbFactory(nameof(this.CreateTagAsync_ShouldCreateTagAndAliases_WhenValidArgumentsProvided));
		var tagModelFactoryMock = new Mock<ITagModelFactory>();

		var expectedTagModelMock = new Mock<ITagModel>();
		tagModelFactoryMock.Setup(f => f.Create(It.IsAny<Tag>())).Returns(expectedTagModelMock.Object);

		var manager = new TagsManager(dbFactory, tagModelFactoryMock.Object);
		var aliases = new List<TagAlias>
		{
			new TagAlias { Alias = "NewAlias1", Ruby = "Ruby1" }
		};

		// Act
		var result = await manager.CreateTagAsync(1, "NewTag", "NewDetail", aliases);

		// Assert
		result.ShouldBe(expectedTagModelMock.Object);

		await using var db = await dbFactory.CreateDbContextAsync();
		var dbTag = await db.Tags.FirstOrDefaultAsync(x => x.TagName == "NewTag");
		dbTag.ShouldNotBeNull();
		dbTag.Detail.ShouldBe("NewDetail");
		dbTag.TagCategoryId.ShouldBe(1);

		var dbAliases = await db.TagAliases.Where(x => x.TagId == dbTag.TagId).ToListAsync();
		dbAliases.Count.ShouldBe(1);
		dbAliases[0].Alias.ShouldBe("NewAlias1");
		dbAliases[0].Ruby.ShouldBe("Ruby1");
	}

	/// <summary>
	/// CreateTagAsyncメソッドがエイリアスにnullが渡された場合にNullReferenceExceptionをスローすることを検証します。
	/// （異常系）
	/// </summary>
	[Fact]
	public async Task CreateTagAsync_ShouldThrowNullReferenceException_WhenAliasesIsNull() {
		// Arrange
		var dbFactory = this.CreateInMemoryDbFactory(nameof(this.CreateTagAsync_ShouldThrowNullReferenceException_WhenAliasesIsNull));
		var tagModelFactoryMock = new Mock<ITagModelFactory>();
		var manager = new TagsManager(dbFactory, tagModelFactoryMock.Object);

		// Act & Assert
		await Should.ThrowAsync<ArgumentNullException>(() => manager.CreateTagAsync(1, "NewTag", "NewDetail", null!));
	}

	/// <summary>
	/// AddTagAsyncメソッドがタグを含まないファイルに対してタグを追加し、変更を保存することを検証します。
	/// </summary>
	[Fact]
	public async Task AddTagAsync_ShouldAddTagToFiles_WhenFilesDoNotHaveTag() {
		// Arrange
		var dbFactory = this.CreateInMemoryDbFactory(nameof(this.AddTagAsync_ShouldAddTagToFiles_WhenFilesDoNotHaveTag));
		var tagModelFactoryMock = new Mock<ITagModelFactory>();
		var manager = new TagsManager(dbFactory, tagModelFactoryMock.Object);

		var tagMock = new Mock<ITagModel>();
		tagMock.SetupGet(x => x.TagId).Returns(99);

		var fileTags = new List<ITagModel>();
		var fileModelMock = new Mock<IFileModel>();
		fileModelMock.SetupGet(x => x.Id).Returns(100);
		fileModelMock.SetupGet(x => x.Tags).Returns(fileTags);

		await using (var dbSetup = await dbFactory.CreateDbContextAsync()) {
			dbSetup.MediaFiles.Add(new MediaFile { MediaFileId = 100, FilePath = "test", DirectoryPath = "dir", Description = "" });
			dbSetup.Tags.Add(new Tag { TagId = 99, TagName = "Tag99", Detail = "", TagCategory = null! });
			await dbSetup.SaveChangesAsync();
		}

		// Act
		await manager.AddTagAsync([fileModelMock.Object], tagMock.Object);

		// Assert
		fileTags.ShouldContain(tagMock.Object);
		await using var db = await dbFactory.CreateDbContextAsync();
		var rel = await db.MediaFileTags.FirstOrDefaultAsync(x => x.MediaFileId == 100 && x.TagId == 99);
		rel.ShouldNotBeNull();
	}

	/// <summary>
	/// AddTagAsyncメソッドがすでにタグを持っているファイルに対しては何も処理を行わないことを検証します。
	/// </summary>
	[Fact]
	public async Task AddTagAsync_ShouldDoNothing_WhenFilesAlreadyHaveTag() {
		// Arrange
		var dbFactory = this.CreateInMemoryDbFactory(nameof(this.AddTagAsync_ShouldDoNothing_WhenFilesAlreadyHaveTag));
		var tagModelFactoryMock = new Mock<ITagModelFactory>();
		var manager = new TagsManager(dbFactory, tagModelFactoryMock.Object);

		var tagMock = new Mock<ITagModel>();
		tagMock.SetupGet(x => x.TagId).Returns(99);

		var fileTags = new List<ITagModel> { tagMock.Object };
		var fileModelMock = new Mock<IFileModel>();
		fileModelMock.SetupGet(x => x.Id).Returns(100);
		fileModelMock.SetupGet(x => x.Tags).Returns(fileTags);

		// Act
		await manager.AddTagAsync([fileModelMock.Object], tagMock.Object);

		// Assert
		await using var db = await dbFactory.CreateDbContextAsync();
		var rel = await db.MediaFileTags.FirstOrDefaultAsync(x => x.MediaFileId == 100 && x.TagId == 99);
		rel.ShouldBeNull(); // DBには追加されないこと
	}

	/// <summary>
	/// AddTagAsyncメソッドの引数tagにnullを渡した場合、LINQ実行時にNullReferenceExceptionが発生することを検証します。
	/// （実装のバグの確認・異常系）
	/// </summary>
	[Fact]
	public async Task AddTagAsync_ShouldThrowNullReferenceException_WhenTagIsNull() {
		// Arrange
		var dbFactory = this.CreateInMemoryDbFactory(nameof(this.AddTagAsync_ShouldThrowNullReferenceException_WhenTagIsNull));
		var tagModelFactoryMock = new Mock<ITagModelFactory>();
		var manager = new TagsManager(dbFactory, tagModelFactoryMock.Object);

		var fileModelMock = new Mock<IFileModel>();
		var tagMock = new Mock<ITagModel>();
		tagMock.SetupGet(t => t.TagId).Returns(1);
		fileModelMock.SetupGet(x => x.Tags).Returns(new List<ITagModel> { tagMock.Object });

		// Act & Assert
		await Should.ThrowAsync<NullReferenceException>(() => manager.AddTagAsync([fileModelMock.Object], null!));
	}

	/// <summary>
	/// RemoveTagAsyncメソッドが指定されたタグを持つファイルからタグとリレーションを削除することを検証します。
	/// </summary>
	[Fact]
	public async Task RemoveTagAsync_ShouldRemoveTagFromFiles_WhenFilesHaveTag() {
		// Arrange
		var dbFactory = this.CreateInMemoryDbFactory(nameof(this.RemoveTagAsync_ShouldRemoveTagFromFiles_WhenFilesHaveTag));
		var tagModelFactoryMock = new Mock<ITagModelFactory>();
		var manager = new TagsManager(dbFactory, tagModelFactoryMock.Object);

		var tagMock = new Mock<ITagModel>();
		tagMock.SetupGet(x => x.TagId).Returns(99);

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

		// Act
		await manager.RemoveTagAsync([fileModelMock.Object], 99);

		// Assert
		fileTags.ShouldBeEmpty();
		await using var db = await dbFactory.CreateDbContextAsync();
		var rel = await db.MediaFileTags.FirstOrDefaultAsync(x => x.MediaFileId == 100 && x.TagId == 99);
		rel.ShouldBeNull();
	}

	/// <summary>
	/// RemoveTagAsyncメソッドが指定されたタグを持たないファイルに対しては何も処理を行わないことを検証します。
	/// </summary>
	[Fact]
	public async Task RemoveTagAsync_ShouldDoNothing_WhenFilesDoNotHaveTag() {
		// Arrange
		var dbFactory = this.CreateInMemoryDbFactory(nameof(this.RemoveTagAsync_ShouldDoNothing_WhenFilesDoNotHaveTag));
		var tagModelFactoryMock = new Mock<ITagModelFactory>();
		var manager = new TagsManager(dbFactory, tagModelFactoryMock.Object);

		var fileTags = new List<ITagModel>();
		var fileModelMock = new Mock<IFileModel>();
		fileModelMock.SetupGet(x => x.Id).Returns(100);
		fileModelMock.SetupGet(x => x.Tags).Returns(fileTags);

		// Act
		await manager.RemoveTagAsync([fileModelMock.Object], 99);

		// Assert
		// No exceptions thrown
	}

	/// <summary>
	/// UpdateTagAsyncメソッドが既存タグの情報とエイリアスを正しく更新することを検証します。
	/// </summary>
	[Fact]
	public async Task UpdateTagAsync_ShouldUpdateTagAndAliases_WhenTagExists() {
		// Arrange
		var dbFactory = this.CreateInMemoryDbFactory(nameof(this.UpdateTagAsync_ShouldUpdateTagAndAliases_WhenTagExists));
		await this.SeedDatabaseAsync(dbFactory);

		var tagModelFactoryMock = new Mock<ITagModelFactory>();
		var manager = new TagsManager(dbFactory, tagModelFactoryMock.Object);
		var newAliases = new List<TagAlias>
		{
			new TagAlias { Alias = "UpdatedAlias1", Ruby = "UpdatedRuby1" }
		};

		// Act
		await manager.UpdateTagAsync(1, 2, "UpdatedTag1", "UpdatedDetail1", newAliases);

		// Assert
		await using var db = await dbFactory.CreateDbContextAsync();
		var dbTag = await db.Tags.FirstAsync(x => x.TagId == 1);
		dbTag.TagCategoryId.ShouldBe(2);
		dbTag.TagName.ShouldBe("UpdatedTag1");
		dbTag.Detail.ShouldBe("UpdatedDetail1");

		var dbAliases = await db.TagAliases.Where(x => x.TagId == 1).ToListAsync();
		dbAliases.Count.ShouldBe(1);
		dbAliases[0].Alias.ShouldBe("UpdatedAlias1");
		dbAliases[0].Ruby.ShouldBe("UpdatedRuby1");
	}

	/// <summary>
	/// UpdateTagAsyncメソッドが存在しないタグIDを更新しようとした場合にInvalidOperationExceptionをスローすることを検証します。
	/// （異常系）
	/// </summary>
	[Fact]
	public async Task UpdateTagAsync_ShouldThrowInvalidOperationException_WhenTagDoesNotExist() {
		// Arrange
		var dbFactory = this.CreateInMemoryDbFactory(nameof(this.UpdateTagAsync_ShouldThrowInvalidOperationException_WhenTagDoesNotExist));
		var tagModelFactoryMock = new Mock<ITagModelFactory>();
		var manager = new TagsManager(dbFactory, tagModelFactoryMock.Object);

		// Act & Assert
		await Should.ThrowAsync<InvalidOperationException>(() => manager.UpdateTagAsync(999, 1, "Name", "Detail", []));
	}

	/// <summary>
	/// UpdateTagCategoryAsyncメソッドが既存のタグ分類を更新することを検証します。
	/// </summary>
	[Fact]
	public async Task UpdateTagCategoryAsync_ShouldUpdateCategory_WhenCategoryExists() {
		// Arrange
		var dbFactory = this.CreateInMemoryDbFactory(nameof(this.UpdateTagCategoryAsync_ShouldUpdateCategory_WhenCategoryExists));
		await this.SeedDatabaseAsync(dbFactory);

		var tagModelFactoryMock = new Mock<ITagModelFactory>();
		var manager = new TagsManager(dbFactory, tagModelFactoryMock.Object);

		// Act
		await manager.UpdateTagCategoryAsync(1, "UpdatedCategoryName", "UpdatedCatDetail");

		// Assert
		await using var db = await dbFactory.CreateDbContextAsync();
		var dbCat = await db.TagCategories.FirstAsync(x => x.TagCategoryId == 1);
		dbCat.TagCategoryName.ShouldBe("UpdatedCategoryName");
		dbCat.Detail.ShouldBe("UpdatedCatDetail");
	}

	/// <summary>
	/// UpdateTagCategoryAsyncメソッドが存在しないタグ分類IDの場合、新規に作成することを検証します。
	/// </summary>
	[Fact]
	public async Task UpdateTagCategoryAsync_ShouldCreateCategory_WhenCategoryDoesNotExist() {
		// Arrange
		var dbFactory = this.CreateInMemoryDbFactory(nameof(this.UpdateTagCategoryAsync_ShouldCreateCategory_WhenCategoryDoesNotExist));
		var tagModelFactoryMock = new Mock<ITagModelFactory>();
		var manager = new TagsManager(dbFactory, tagModelFactoryMock.Object);

		// Act
		await manager.UpdateTagCategoryAsync(2, "NewCategoryName", "NewCatDetail");

		// Assert
		await using var db = await dbFactory.CreateDbContextAsync();
		var dbCat = await db.TagCategories.FirstOrDefaultAsync(x => x.TagCategoryId == 2);
		dbCat.ShouldNotBeNull();
		dbCat.TagCategoryName.ShouldBe("NewCategoryName");
		dbCat.Detail.ShouldBe("NewCatDetail");
	}

	/// <summary>
	/// Loadメソッドがデータベースからタグとタグ分類を読み込み、プロパティに設定することを検証します。
	/// </summary>
	[Fact]
	public async Task Load_ShouldLoadTagsAndCategoriesFromDatabase() {
		// Arrange
		var dbFactory = this.CreateInMemoryDbFactory(nameof(this.Load_ShouldLoadTagsAndCategoriesFromDatabase));
		await this.SeedDatabaseAsync(dbFactory);

		var tagModelFactoryMock = new Mock<ITagModelFactory>();
		var tagModel1 = new Mock<ITagModel>().Object;
		var tagModel2 = new Mock<ITagModel>().Object;

		tagModelFactoryMock.Setup(f => f.Create(It.Is<Tag>(t => t.TagId == 1))).Returns(tagModel1);
		tagModelFactoryMock.Setup(f => f.Create(It.Is<Tag>(t => t.TagId == 2))).Returns(tagModel2);

		var manager = new TagsManager(dbFactory, tagModelFactoryMock.Object);

		// Act
		await manager.Load();

		// Assert
		manager.TagCategories.Count.ShouldBe(1);
		manager.TagCategories[0].TagCategoryId.ShouldBe(1);

		manager.Tags.Count.ShouldBe(2);
		manager.Tags.ShouldContain(tagModel1);
		manager.Tags.ShouldContain(tagModel2);
	}
}