using MediaDeck.Composition.Interfaces.MediaItemTypes.Models;
using MediaDeck.Composition.Interfaces.Tags;
using MediaDeck.Core.Models.Files;
using Moq;
using ObservableCollections;
using Shouldly;

namespace MediaDeck.Core.Tests.Models.Files;

/// <summary>
/// <see cref="TagsDetailModel"/> の単体テスト。
/// </summary>
public class TagsDetailModelTests : IDisposable {
	private readonly Mock<ITagsManager> _tagsManagerMock;
	private readonly TagsDetailModel _sut;

	public TagsDetailModelTests() {
		this._tagsManagerMock = new Mock<ITagsManager>();
		this._tagsManagerMock.Setup(x => x.Tags).Returns(new ObservableList<ITagModel>());
		this._tagsManagerMock.Setup(x => x.TagCategories).Returns(new ObservableList<ITagCategoryModel>());
		this._sut = new TagsDetailModel(this._tagsManagerMock.Object);
	}

	public void Dispose() {
		this._sut.Dispose();
	}

	[Fact]
	public void Tags_IsInitiallyEmpty() {
		this._sut.Tags.Count.ShouldBe(0);
	}

	[Fact]
	public void Refresh_GroupsAndCountsTags() {
		var tag1 = new Mock<ITagModel>();
		tag1.SetupGet(x => x.TagId).Returns(1);
		var tag2 = new Mock<ITagModel>();
		tag2.SetupGet(x => x.TagId).Returns(2);

		var fileMock1 = new Mock<IMediaItemModel>();
		fileMock1.SetupGet(x => x.Tags).Returns([tag1.Object, tag2.Object]);

		var fileMock2 = new Mock<IMediaItemModel>();
		fileMock2.SetupGet(x => x.Tags).Returns([tag1.Object]);

		this._sut.Refresh([fileMock1.Object, fileMock2.Object]);

		this._sut.Tags.Count.ShouldBe(2);
		this._sut.Tags.First(x => x.Value.TagId == 1).Count.ShouldBe(2);
		this._sut.Tags.First(x => x.Value.TagId == 2).Count.ShouldBe(1);
	}

	[Fact]
	public async Task FindTagByNameAsync_DelegatesToTagsManager() {
		var tagMock = new Mock<ITagModel>();
		tagMock.SetupGet(x => x.TagName).Returns("TestTag");
		this._tagsManagerMock.Setup(x => x.FindTagByNameAsync("TestTag")).ReturnsAsync(tagMock.Object);

		var result = await this._sut.FindTagByNameAsync("TestTag");

		result.ShouldBe(tagMock.Object);
		this._tagsManagerMock.Verify(x => x.FindTagByNameAsync("TestTag"), Times.Once);
	}

	[Fact]
	public async Task AddTagAsync_DelegatesToTagsManager() {
		var tagMock = new Mock<ITagModel>();
		tagMock.SetupGet(x => x.TagId).Returns(1);

		var fileMock = new Mock<IMediaItemModel>();
		fileMock.SetupGet(x => x.Tags).Returns([tagMock.Object]);

		await this._sut.AddTagAsync([fileMock.Object], tagMock.Object);

		this._tagsManagerMock.Verify(x => x.AddTagAsync(It.IsAny<IMediaItemModel[]>(), tagMock.Object), Times.Once);
	}

	[Fact]
	public async Task RemoveTagAsync_DelegatesToTagsManager() {
		var fileMock = new Mock<IMediaItemModel>();
		fileMock.SetupGet(x => x.Tags).Returns([]);

		await this._sut.RemoveTagAsync([fileMock.Object], 1);

		this._tagsManagerMock.Verify(x => x.RemoveTagAsync(It.IsAny<IMediaItemModel[]>(), 1), Times.Once);
	}
}