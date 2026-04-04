using FluentAssertions;
using MediaDeck.Composition.Objects;

namespace MediaDeck.Core.Tests.Objects;

public class ComparableSizeTests {
	[Fact]
	public void Constructor_SetsPropertiesAndCalculatesArea() {
		var size = new ComparableSize(10, 20);
		size.Width.Should().Be(10);
		size.Height.Should().Be(20);
		size.Area.Should().Be(200);
	}

	[Fact]
	public void SetWidth_UpdatesArea() {
		var size = new ComparableSize(10, 20);
		size.Width = 30;
		size.Area.Should().Be(600);
	}

	[Fact]
	public void SetHeight_UpdatesArea() {
		var size = new ComparableSize(10, 20);
		size.Height = 30;
		size.Area.Should().Be(300);
	}

	[Fact]
	public void Area_ShouldBeNaN_WhenWidthIsNaN() {
		var size = new ComparableSize(double.NaN, 20);
		size.Area.Should().Be(double.NaN);
	}

	[Fact]
	public void Area_ShouldBeNaN_WhenHeightIsNaN() {
		var size = new ComparableSize(10, double.NaN);
		size.Area.Should().Be(double.NaN);
	}

	[Fact]
	public void CompareTo_ReturnsCorrectOrder() {
		var size1 = new ComparableSize(10, 10); // Area 100
		var size2 = new ComparableSize(5, 30);  // Area 150
		var size3 = new ComparableSize(20, 5);  // Area 100

		size1.CompareTo(size2).Should().BeNegative();
		size2.CompareTo(size1).Should().BePositive();
		size1.CompareTo(size3).Should().Be(0);
	}

	[Fact]
	public void Operators_WorkCorrectly() {
		var size1 = new ComparableSize(10, 10);
		var size2 = new ComparableSize(5, 30);

		(size1 < size2).Should().BeTrue();
		(size1 > size2).Should().BeFalse();
		(size1 <= size2).Should().BeTrue();
		(size1 >= size2).Should().BeFalse();
		(size1 == size2).Should().BeFalse();
		(size1 != size2).Should().BeTrue();
	}

	[Fact]
	public void Equals_WorksCorrectly() {
		var size1 = new ComparableSize(10, 20);
		var size2 = new ComparableSize(10, 20);
		var size3 = new ComparableSize(20, 10);

		size1.Equals(size2).Should().BeTrue();
		size1.Equals(size3).Should().BeFalse();
		size1.Equals(null).Should().BeFalse();
		size1.Equals("not a size").Should().BeFalse();
	}

	[Fact]
	public void GetHashCode_IsConsistent() {
		var size1 = new ComparableSize(10, 20);
		var size2 = new ComparableSize(10, 20);

		size1.GetHashCode().Should().Be(size2.GetHashCode());
	}

	[Fact]
	public void ToString_ReturnsCorrectFormat() {
		var size = new ComparableSize(10, 20);
		size.ToString().Should().Be("10x20");
	}
}
