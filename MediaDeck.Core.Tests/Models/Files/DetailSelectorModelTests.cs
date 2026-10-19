using MediaDeck.Composition.Interfaces.MediaItemTypes.Models;
using MediaDeck.Composition.Interfaces.Tags;
using MediaDeck.Core.Models.Files;
using Moq;
using ObservableCollections;
using R3;
using Shouldly;

namespace MediaDeck.Core.Tests.Models.Files;

/// <summary>
/// <see cref="DetailSelectorModel"/> の単体テスト。
/// </summary>
public class DetailSelectorModelTests : IDisposable {
	private readonly PropertiesDetailModel _propertiesModel;
	private readonly TagsDetailModel _tagsModel;
	private readonly DetailSelectorModel _sut;

	public DetailSelectorModelTests() {
		var tagsManagerMock = new Mock<ITagsManager>();
		tagsManagerMock.Setup(x => x.Tags).Returns(new ObservableList<ITagModel>());
		tagsManagerMock.Setup(x => x.TagCategories).Returns(new ObservableList<ITagCategoryModel>());
		this._propertiesModel = new PropertiesDetailModel();
		this._tagsModel = new TagsDetailModel(tagsManagerMock.Object);
		this._sut = new DetailSelectorModel(this._propertiesModel, this._tagsModel);
	}

	public void Dispose() {
		this._sut.Dispose();
	}

	[Fact]
	public void Refresh_EmptyArray_ClearsBothSubModels() {
		this._sut.Refresh([]);
		this._sut.PropertiesModel.Properties.Value.ShouldBeEmpty();
		this._sut.PropertiesModel.Rate.Value.ShouldBe(0);
		this._sut.PropertiesModel.UsageCount.Value.ShouldBe(0);
		this._sut.TagsModel.Tags.ShouldBeEmpty();
	}

	[Fact]
	public void Refresh_WithFile_RefreshesBothSubModels() {
		var tagMock = new Mock<ITagModel>();
		tagMock.SetupGet(x => x.TagId).Returns(1);

		var fileMock = new Mock<IMediaItemModel>();
		fileMock.Setup(x => x.Properties).Returns([]);
		fileMock.Setup(x => x.Rate).Returns(4);
		fileMock.Setup(x => x.UsageCount).Returns(10);
		fileMock.Setup(x => x.FilePath).Returns("/test.jpg");
		fileMock.Setup(x => x.Description).Returns("desc");
		fileMock.Setup(x => x.Tags).Returns([tagMock.Object]);
		fileMock.Setup(x => x.Changed).Returns(Observable.Empty<Unit>());

		this._sut.Refresh([fileMock.Object]);

		this._sut.PropertiesModel.Rate.Value.ShouldBe(4);
		this._sut.PropertiesModel.UsageCount.Value.ShouldBe(10);
		this._sut.PropertiesModel.RepresentativeFilePath.Value.ShouldBe("/test.jpg");
		this._sut.PropertiesModel.Description.Value.ShouldBe("desc");
		this._sut.TagsModel.Tags.Count.ShouldBe(1);
	}

	[Fact]
	public void ContentChanged_IsNotNull() {
		this._sut.ContentChanged.ShouldNotBeNull();
	}
}