using System;
using System.Drawing;
using Shouldly;
using MediaDeck.Composition.Interfaces.FileTypes.Models;
using MediaDeck.Composition.Interfaces.Primitives;
using MediaDeck.Core.Models.Maps;
using Moq;
using Xunit;
using Rectangle = MediaDeck.Core.Models.Maps.Rectangle;

namespace MediaDeck.Core.Tests.Models.Maps;

/// <summary>
/// <see cref="MapPin"/> のテストクラス
/// </summary>
public class MapPinTests {
    [Fact]
    public void Constructor_SetsPropertiesCorrectly() {
        // Arrange
        var mockFile = new Mock<IFileModel>();
        var mockLocation = new Mock<IGpsLocation>();
        mockFile.Setup(f => f.Location).Returns(mockLocation.Object);
        var rect = new Rectangle(new Point(10, 20), new Size(30, 40));

        // Act
        var mapPin = new MapPin(mockFile.Object, rect);

        // Assert
        mapPin.Core.Value.ShouldBe(mockFile.Object);
        mapPin.CoreRectangle.ShouldBe(rect);
        mapPin.Location.ShouldBe(mockLocation.Object);
        mapPin.Items.ShouldHaveSingleItem().ShouldBe(mockFile.Object);
        mapPin.Count.ShouldBe(1);
        mapPin.PinState.Value.ShouldBe(PinState.Unselected);
    }

    [Fact]
    public void Items_CountChanged_UpdatesCountProperty() {
        // Arrange
        var mockFile1 = new Mock<IFileModel>();
        var mockFile2 = new Mock<IFileModel>();
        var rect = new Rectangle(new Point(0, 0), new Size(100, 100));
        var mapPin = new MapPin(mockFile1.Object, rect);

        // Act
        mapPin.Items.Add(mockFile2.Object);

        // Assert
        mapPin.Count.ShouldBe(2);
    }

    [Fact]
    public void ToString_ReturnsExpectedFormat() {
        // Arrange
        var mockFile = new Mock<IFileModel>();
        mockFile.Setup(f => f.FilePath).Returns("test/path/to/file.jpg");
        var rect = new Rectangle(new Point(0, 0), new Size(100, 100));
        var mapPin = new MapPin(mockFile.Object, rect);

        // Act
        var result = mapPin.ToString();

        // Assert
        result.ShouldBe("<[MediaDeck.Core.Models.Maps.MapPin] test/path/to/file.jpg>");
    }
}
