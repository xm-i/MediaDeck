using MediaDeck.Composition.Objects;

namespace MediaDeck.Core.Tests.Objects;

public class ComparableSizeTests {
	[Fact]
	public void Constructor_SetsPropertiesAndCalculatesArea() {
		ComparableSize size = new(10, 20);
		Assert.Equal(10, size.Width);
		Assert.Equal(20, size.Height);
		Assert.Equal(200, size.Area);
	}

	[Fact]
	public void SetWidth_UpdatesArea() {
		ComparableSize size = new(10, 20);
		size.Width = 30;
		Assert.Equal(600, size.Area);
	}

	[Fact]
	public void SetHeight_UpdatesArea() {
		ComparableSize size = new(10, 20);
		size.Height = 30;
		Assert.Equal(300, size.Area);
	}

	[Fact]
	public void Area_ShouldBeNaN_WhenWidthIsNaN() {
		ComparableSize size = new(double.NaN, 20);
		Assert.True(double.IsNaN(size.Area));
	}

	[Fact]
	public void Area_ShouldBeNaN_WhenHeightIsNaN() {
		ComparableSize size = new(10, double.NaN);
		Assert.True(double.IsNaN(size.Area));
	}

	[Fact]
	public void CompareTo_ReturnsCorrectOrder() {
		ComparableSize size1 = new(10, 10); // Area 100
		ComparableSize size2 = new(5, 30);  // Area 150
		ComparableSize size3 = new(20, 5);  // Area 100

		Assert.True(size1.CompareTo(size2) < 0);
		Assert.True(size2.CompareTo(size1) > 0);
		Assert.Equal(0, size1.CompareTo(size3));
	}

	[Fact]
	public void Operators_WorkCorrectly() {
		ComparableSize size1 = new(10, 10);
		ComparableSize size2 = new(5, 30);

		Assert.True(size1 < size2);
		Assert.False(size1 > size2);
		Assert.True(size1 <= size2);
		Assert.False(size1 >= size2);
		Assert.False(size1 == size2);
		Assert.True(size1 != size2);
	}

	[Fact]
	public void Equals_WorksCorrectly() {
		ComparableSize size1 = new(10, 20);
		ComparableSize size2 = new(10, 20);
		ComparableSize size3 = new(20, 10);

		Assert.True(size1.Equals(size2));
		Assert.False(size1.Equals(size3));
		Assert.False(size1.Equals(null));
		Assert.False(size1.Equals("not a size"));
	}

	[Fact]
	public void GetHashCode_IsConsistent() {
		ComparableSize size1 = new(10, 20);
		ComparableSize size2 = new(10, 20);

		Assert.Equal(size1.GetHashCode(), size2.GetHashCode());
	}

	[Fact]
	public void ToString_ReturnsCorrectFormat() {
		ComparableSize size = new(10, 20);
		Assert.Equal("10x20", size.ToString());
	}
}
