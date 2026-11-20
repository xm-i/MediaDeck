using MediaDeck.Composition.Objects;
using Shouldly;

namespace MediaDeck.Core.Tests.Composition;

/// <summary>
/// <see cref="ComparableSize"/> の等価性・ハッシュコード検証テスト。
/// </summary>
public class ComparableSizeTests {
	/// <summary>
	/// 同じ幅と高さを持つインスタンスが等価と判定され、同一のハッシュコードを返すことを検証します。
	/// </summary>
	[Fact]
	public void Equals_WithSameValues_ShouldReturnTrueAndMatchHashCode() {
		var a = new ComparableSize(1920, 1080);
		var b = new ComparableSize(1920, 1080);

		a.Equals(b).ShouldBeTrue();
		a.GetHashCode().ShouldBe(b.GetHashCode());
	}

	/// <summary>
	/// 幅または高さが異なるインスタンス、および別型のオブジェクトに対して false を返すことを検証します。
	/// </summary>
	[Fact]
	public void Equals_WithDifferentValues_ShouldReturnFalse() {
		var a = new ComparableSize(1920, 1080);
		var diffWidth = new ComparableSize(1280, 1080);
		var diffHeight = new ComparableSize(1920, 720);

		a.Equals(diffWidth).ShouldBeFalse();
		a.Equals(diffHeight).ShouldBeFalse();
		a.Equals(null).ShouldBeFalse();
		a.Equals("not a size").ShouldBeFalse();
	}

	/// <summary>
	/// double.NaN を含むインスタンス同士の比較で、Equals と GetHashCode の一貫性が保たれることを検証します。
	/// </summary>
	[Fact]
	public void Equals_WithNaN_ShouldHandleNaNConsistently() {
		var a = new ComparableSize(double.NaN, 100);
		var b = new ComparableSize(double.NaN, 100);

		a.Equals(b).ShouldBeTrue();
		a.GetHashCode().ShouldBe(b.GetHashCode());
	}
}