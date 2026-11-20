using MediaDeck.Core.Primitives;
using Shouldly;

namespace MediaDeck.Core.Tests.Primitives;

/// <summary>
///     <see cref="ValueCountPair{T}"/> のテストクラスです。
/// </summary>
public class ValueCountPairTests {
	/// <summary>
	///     コンストラクタで値と件数が正しく設定されることを確認します。
	/// </summary>
	[Fact]
	public void Constructor_SetsValueAndCount() {
		// Arrange & Act
		var pair = new ValueCountPair<string>("test", 42);

		// Assert
		pair.Value.ShouldBe("test");
		pair.Count.ShouldBe(42);
	}

	/// <summary>
	///     同じ値と件数を持つペアが等しいと判定されることを確認します。
	/// </summary>
	[Fact]
	public void Equals_SameValueAndCount_ReturnsTrue() {
		// Arrange
		var a = new ValueCountPair<string>("test", 5);
		var b = new ValueCountPair<string>("test", 5);

		// Act & Assert
		a.Equals(b).ShouldBeTrue();
	}

	/// <summary>
	///     異なる値を持つペアが等しくないと判定されることを確認します。
	/// </summary>
	[Fact]
	public void Equals_DifferentValue_ReturnsFalse() {
		// Arrange
		var a = new ValueCountPair<string>("a", 5);
		var b = new ValueCountPair<string>("b", 5);

		// Act & Assert
		a.Equals(b).ShouldBeFalse();
	}

	/// <summary>
	///     異なる件数を持つペアが等しくないと判定されることを確認します。
	/// </summary>
	[Fact]
	public void Equals_DifferentCount_ReturnsFalse() {
		// Arrange
		var a = new ValueCountPair<string>("test", 1);
		var b = new ValueCountPair<string>("test", 2);

		// Act & Assert
		a.Equals(b).ShouldBeFalse();
	}

	/// <summary>
	///     int型のValueCountPairが正しく動作することを確認します。
	/// </summary>
	[Fact]
	public void Constructor_IntType_WorksCorrectly() {
		// Arrange & Act
		var pair = new ValueCountPair<int>(100, 3);

		// Assert
		pair.Value.ShouldBe(100);
		pair.Count.ShouldBe(3);
	}

	/// <summary>
	///     <see cref="ValueCountPair{T}.Equals(object?)"/> と <see cref="ValueCountPair{T}.GetHashCode()"/> および等価演算子が
	///     同値・異値・null・別型に対して正しく動作することを確認します。
	/// </summary>
	[Fact]
	public void ObjectEqualsGetHashCodeAndOperators_WorkCorrectly() {
		// Arrange
		var a = new ValueCountPair<string>("test", 5);
		var b = new ValueCountPair<string>("test", 5);
		var diff = new ValueCountPair<string>("test", 6);

		// Assert
		a.Equals((object)b).ShouldBeTrue();
		a.Equals((object)diff).ShouldBeFalse();
		a.Equals(null).ShouldBeFalse();
		a.Equals("not a pair").ShouldBeFalse();
		a.GetHashCode().ShouldBe(b.GetHashCode());
		(a == b).ShouldBeTrue();
		(a != b).ShouldBeFalse();
		(a == diff).ShouldBeFalse();
		(a != diff).ShouldBeTrue();
	}
}