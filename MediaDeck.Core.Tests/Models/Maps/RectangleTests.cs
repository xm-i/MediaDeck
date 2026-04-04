using System;
using System.Drawing;
using Shouldly;
using Xunit;
using Rectangle = MediaDeck.Core.Models.Maps.Rectangle;

namespace MediaDeck.Core.Tests.Models.Maps;

/// <summary>
/// <see cref="Rectangle"/> のテストクラス
/// </summary>
public class RectangleTests {
    [Fact]
    public void DistanceTo_OverlappingRectangles_ReturnsZero() {
        var rect1 = new Rectangle(new Point(0, 0), new Size(100, 100));
        var rect2 = new Rectangle(new Point(50, 50), new Size(100, 100));

        var distance = rect1.DistanceTo(rect2);

        distance.ShouldBe(0);
    }

    [Fact]
    public void DistanceTo_AdjacentRectangles_ReturnsZero() {
        var rect1 = new Rectangle(new Point(0, 0), new Size(100, 100));
        var rect2 = new Rectangle(new Point(100, 0), new Size(100, 100));

        var distance = rect1.DistanceTo(rect2);

        distance.ShouldBe(0);
    }

    [Fact]
    public void DistanceTo_RectanglesInsideEachOther_ReturnsZero() {
        var rect1 = new Rectangle(new Point(0, 0), new Size(100, 100));
        var rect2 = new Rectangle(new Point(25, 25), new Size(50, 50));

        var distance1 = rect1.DistanceTo(rect2);
        var distance2 = rect2.DistanceTo(rect1);

        distance1.ShouldBe(0);
        distance2.ShouldBe(0);
    }

    [Fact]
    public void DistanceTo_DistantRectanglesHorizontally_ReturnsCorrectDistance() {
        var rect1 = new Rectangle(new Point(0, 0), new Size(100, 100));
        var rect2 = new Rectangle(new Point(200, 0), new Size(100, 100));

        var distance = rect1.DistanceTo(rect2);

        distance.ShouldBe(100);
    }

    [Fact]
    public void DistanceTo_DistantRectanglesVertically_ReturnsCorrectDistance() {
        var rect1 = new Rectangle(new Point(0, 0), new Size(100, 100));
        var rect2 = new Rectangle(new Point(0, 200), new Size(100, 100));

        var distance = rect1.DistanceTo(rect2);

        distance.ShouldBe(100);
    }

    [Fact]
    public void DistanceTo_DistantRectanglesDiagonally_ReturnsCorrectDistance() {
        var rect1 = new Rectangle(new Point(0, 0), new Size(100, 100));
        var rect2 = new Rectangle(new Point(200, 200), new Size(100, 100));

        var distance = rect1.DistanceTo(rect2);

        distance.ShouldBe(Math.Sqrt(20000), 0.0001);
    }

    [Fact]
    public void DistanceTo_DistantRectanglesLeft_ReturnsCorrectDistance() {
        var rect1 = new Rectangle(new Point(200, 0), new Size(100, 100));
        var rect2 = new Rectangle(new Point(0, 0), new Size(100, 100));

        var distance = rect1.DistanceTo(rect2);

        distance.ShouldBe(100);
    }

    [Fact]
    public void DistanceTo_DistantRectanglesTop_ReturnsCorrectDistance() {
        var rect1 = new Rectangle(new Point(0, 200), new Size(100, 100));
        var rect2 = new Rectangle(new Point(0, 0), new Size(100, 100));

        var distance = rect1.DistanceTo(rect2);

        distance.ShouldBe(100);
    }
}
