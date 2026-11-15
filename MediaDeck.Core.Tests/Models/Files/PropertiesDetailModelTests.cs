using MediaDeck.Composition.Interfaces.MediaItemTypes.Models;
using MediaDeck.Composition.Objects;
using MediaDeck.Core.Models.Files;
using Moq;
using R3;
using Shouldly;

namespace MediaDeck.Core.Tests.Models.Files;

/// <summary>
/// <see cref="PropertiesDetailModel"/> の単体テスト。
/// </summary>
public class PropertiesDetailModelTests : IDisposable {
	private readonly PropertiesDetailModel _sut;

	public PropertiesDetailModelTests() {
		this._sut = new PropertiesDetailModel();
	}

	public void Dispose() {
		this._sut.Dispose();
	}

	[Fact]
	public void Properties_IsInitiallyEmpty() {
		this._sut.Properties.Value.ShouldBeEmpty();
	}

	[Fact]
	public void Rate_DefaultIsZero() {
		this._sut.Rate.Value.ShouldBe(0);
	}

	[Fact]
	public void RepresentativeFilePath_DefaultIsEmpty() {
		this._sut.RepresentativeFilePath.Value.ShouldBeEmpty();
	}

	[Fact]
	public void Refresh_EmptyArray_ClearsProperties() {
		this._sut.Refresh([]);
		this._sut.Properties.Value.ShouldBeEmpty();
		this._sut.Rate.Value.ShouldBe(0);
		this._sut.UsageCount.Value.ShouldBe(0);
		this._sut.RepresentativeFilePath.Value.ShouldBeEmpty();
		this._sut.Description.Value.ShouldBeEmpty();
		this._sut.Metadata.Value.ShouldBeNull();
	}

	[Fact]
	public void Refresh_SingleFile_SetsProperties() {
		var fileMock = new Mock<IMediaItemModel>();
		fileMock.SetupGet(x => x.Rate).Returns(3);
		fileMock.SetupGet(x => x.UsageCount).Returns(10);
		fileMock.SetupGet(x => x.FilePath).Returns("C:\\test\\file.jpg");
		fileMock.SetupGet(x => x.Description).Returns("Test Description");
		fileMock.SetupGet(x => x.Properties).Returns([new TitleValuePair<string>("Title1", "Value1")]);
		fileMock.SetupGet(x => x.Metadata).Returns((MediaDeck.Composition.Tables.Metadata.MediaMetadata?)null);
		fileMock.SetupGet(x => x.Changed).Returns(new Subject<Unit>());

		this._sut.Refresh([fileMock.Object]);

		this._sut.Rate.Value.ShouldBe(3.0);
		this._sut.UsageCount.Value.ShouldBe(10);
		this._sut.RepresentativeFilePath.Value.ShouldBe("C:\\test\\file.jpg");
		this._sut.Description.Value.ShouldBe("Test Description");
		this._sut.Properties.Value.Length.ShouldBe(1);
		this._sut.Properties.Value[0].Title.ShouldBe("Title1");
		this._sut.Properties.Value[0].Values.ShouldNotBeNull();
	}

	[Fact]
	public void Refresh_MultipleFiles_AggregatesProperties() {
		var fileMock1 = new Mock<IMediaItemModel>();
		fileMock1.SetupGet(x => x.Rate).Returns(2);
		fileMock1.SetupGet(x => x.UsageCount).Returns(5);
		fileMock1.SetupGet(x => x.FilePath).Returns("C:\\test\\file1.jpg");
		fileMock1.SetupGet(x => x.Description).Returns("Desc1");
		fileMock1.SetupGet(x => x.Properties).Returns([new TitleValuePair<string>("Res", "1920x1080")]);
		fileMock1.SetupGet(x => x.Changed).Returns(new Subject<Unit>());

		var fileMock2 = new Mock<IMediaItemModel>();
		fileMock2.SetupGet(x => x.Rate).Returns(4);
		fileMock2.SetupGet(x => x.UsageCount).Returns(15);
		fileMock2.SetupGet(x => x.FilePath).Returns("C:\\test\\file2.jpg");
		fileMock2.SetupGet(x => x.Description).Returns("Desc2");
		fileMock2.SetupGet(x => x.Properties).Returns([new TitleValuePair<string>("Res", "1920x1080"), new TitleValuePair<string>("Author", "Alice")]);
		fileMock2.SetupGet(x => x.Changed).Returns(new Subject<Unit>());

		this._sut.Refresh([fileMock1.Object, fileMock2.Object]);

		this._sut.Rate.Value.ShouldBe(3.0);
		this._sut.UsageCount.Value.ShouldBe(10);
		this._sut.RepresentativeFilePath.Value.ShouldBe(string.Empty);
		this._sut.Description.Value.ShouldBe(string.Empty);

		var props = this._sut.Properties.Value;
		props.Length.ShouldBe(2);
		var resProp = props.First(x => x.Title == "Res");
		var resValues = resProp.Values.ToArray();
		resValues.Length.ShouldBe(1);
		resValues[0].Value.ShouldBe("1920x1080");
		resValues[0].Count.ShouldBe(2);

		var authorProp = props.First(x => x.Title == "Author");
		var authorValues = authorProp.Values.ToArray();
		authorValues.Length.ShouldBe(1);
		authorValues[0].Value.ShouldBe("Alice");
		authorValues[0].Count.ShouldBe(1);
	}

	[Fact]
	public void Refresh_SubscribesToFileChanged_AndAutoRefreshes() {
		var changedSubject = new Subject<Unit>();
		var currentRate = 2;
		var fileMock = new Mock<IMediaItemModel>();
		fileMock.SetupGet(x => x.Rate).Returns(() => currentRate);
		fileMock.SetupGet(x => x.UsageCount).Returns(1);
		fileMock.SetupGet(x => x.FilePath).Returns("C:\\test\\file.jpg");
		fileMock.SetupGet(x => x.Description).Returns("Desc");
		fileMock.SetupGet(x => x.Properties).Returns([]);
		fileMock.SetupGet(x => x.Changed).Returns(changedSubject);

		this._sut.Refresh([fileMock.Object]);
		this._sut.Rate.Value.ShouldBe(2.0);

		currentRate = 5;
		changedSubject.OnNext(Unit.Default);

		this._sut.Rate.Value.ShouldBe(5.0);
	}

	[Fact]
	public async Task UpdateDescriptionAsync_DelegatesToFile() {
		var fileMock = new Mock<IMediaItemModel>();
		fileMock.Setup(x => x.UpdateDescriptionAsync("New Desc")).Returns(Task.CompletedTask);

		await this._sut.UpdateDescriptionAsync(fileMock.Object, "New Desc");

		fileMock.Verify(x => x.UpdateDescriptionAsync("New Desc"), Times.Once);
	}

	[Fact]
	public async Task UpdateRateAsync_DelegatesToFiles() {
		var fileMock1 = new Mock<IMediaItemModel>();
		var fileMock2 = new Mock<IMediaItemModel>();
		fileMock1.Setup(x => x.UpdateRateAsync(4)).Returns(Task.CompletedTask);
		fileMock2.Setup(x => x.UpdateRateAsync(4)).Returns(Task.CompletedTask);

		await this._sut.UpdateRateAsync([fileMock1.Object, fileMock2.Object], 4);

		fileMock1.Verify(x => x.UpdateRateAsync(4), Times.Once);
		fileMock2.Verify(x => x.UpdateRateAsync(4), Times.Once);
	}
}