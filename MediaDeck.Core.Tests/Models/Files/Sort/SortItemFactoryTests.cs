using System.ComponentModel;

using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Stores.State.Model.Objects;
using MediaDeck.Core.Models.Files.Sort;
using Shouldly;

namespace MediaDeck.Core.Tests.Models.Files.Sort;

/// <summary>
///     <see cref="SortItemFactory"/> のテストクラスです。
/// </summary>
public class SortItemFactoryTests {
	/// <summary>
	///     全ての有効なSortItemKeyに対してSortItemが正しく生成されることを確認します。
	/// </summary>
	[Theory]
	[InlineData(SortItemKey.FilePath)]
	[InlineData(SortItemKey.CreationTime)]
	[InlineData(SortItemKey.ModifiedTime)]
	[InlineData(SortItemKey.LastAccessTime)]
	[InlineData(SortItemKey.RegisteredTime)]
	[InlineData(SortItemKey.FileSize)]
	[InlineData(SortItemKey.Rate)]
	[InlineData(SortItemKey.Location)]
	[InlineData(SortItemKey.Resolution)]
	[InlineData(SortItemKey.UsageCount)]
	[InlineData(SortItemKey.Duration)]
	[InlineData(SortItemKey.Random)]
	public void Create_ValidKey_ReturnsSortItemWithCorrectKey(SortItemKey key) {
		// Arrange
		var obj = new SortItemObject { SortItemKey = key, Direction = ListSortDirection.Ascending };

		// Act
		var result = SortItemFactory.Create(obj);

		// Assert
		result.ShouldNotBeNull();
		result.Key.ShouldBe(key);
		result.Direction.ShouldBe(ListSortDirection.Ascending);
	}

	/// <summary>
	///     ソート方向がDescendingの場合、正しく設定されることを確認します。
	/// </summary>
	[Fact]
	public void Create_DescendingDirection_SetsDirectionCorrectly() {
		// Arrange
		var obj = new SortItemObject { SortItemKey = SortItemKey.Rate, Direction = ListSortDirection.Descending };

		// Act
		var result = SortItemFactory.Create(obj);

		// Assert
		result.Direction.ShouldBe(ListSortDirection.Descending);
	}

	/// <summary>
	///     無効なSortItemKeyの場合、ArgumentExceptionがスローされることを確認します。
	/// </summary>
	[Fact]
	public void Create_InvalidKey_ThrowsArgumentException() {
		// Arrange
		var obj = new SortItemObject { SortItemKey = (SortItemKey)9999, Direction = ListSortDirection.Ascending };

		// Act & Assert
		Should.Throw<ArgumentException>(() => SortItemFactory.Create(obj));
	}
	/// <summary>
	///     SortItemKey.Random の場合、指定したシード値を持つ RandomSortItem が生成されることを確認します。
	/// </summary>
	[Fact]
	public void Create_RandomKey_ReturnsRandomSortItemWithSeed() {
		// Arrange
		var obj = new SortItemObject { SortItemKey = SortItemKey.Random, Direction = ListSortDirection.Ascending };

		// Act
		var result = SortItemFactory.Create(obj, seed: 12345);

		// Assert
		result.ShouldBeOfType<RandomSortItem>();
		var randomItem = (RandomSortItem)result;
		randomItem.Seed.ShouldBe(12345);
	}
}