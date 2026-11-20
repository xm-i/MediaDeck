namespace MediaDeck.Core.Primitives;

/// <summary>
/// 値と件数のペア
/// </summary>
/// <typeparam name="T"></typeparam>
public readonly struct ValueCountPair<T> : IEquatable<ValueCountPair<T>>, IDisposable {
	public ValueCountPair(T value, int count) {
		this.Value = value;
		this.Count = count;
	}

	/// <summary>
	/// 値
	/// </summary>
	public T Value {
		get;
	}

	/// <summary>
	/// 件数
	/// </summary>
	public int Count {
		get;
	}

	/// <inheritdoc/>
	public bool Equals(ValueCountPair<T> other) {
		return EqualityComparer<T>.Default.Equals(this.Value, other.Value) && this.Count == other.Count;
	}

	/// <inheritdoc/>
	public override bool Equals(object? obj) {
		return obj is ValueCountPair<T> other && this.Equals(other);
	}

	/// <inheritdoc/>
	public override int GetHashCode() {
		return HashCode.Combine(this.Value, this.Count);
	}

	/// <summary>
	/// 2つの <see cref="ValueCountPair{T}"/> が等しいかどうかを判定します。
	/// </summary>
	/// <param name="left">比較元の値。</param>
	/// <param name="right">比較先の値。</param>
	/// <returns>等しい場合は <c>true</c>、それ以外は <c>false</c>。</returns>
	public static bool operator ==(ValueCountPair<T> left, ValueCountPair<T> right) {
		return left.Equals(right);
	}

	/// <summary>
	/// 2つの <see cref="ValueCountPair{T}"/> が等しくないかどうかを判定します。
	/// </summary>
	/// <param name="left">比較元の値。</param>
	/// <param name="right">比較先の値。</param>
	/// <returns>等しくない場合は <c>true</c>、それ以外は <c>false</c>。</returns>
	public static bool operator !=(ValueCountPair<T> left, ValueCountPair<T> right) {
		return !left.Equals(right);
	}

	/// <summary>
	/// 保持している値が <see cref="IDisposable"/> の場合にリソースを破棄します。
	/// </summary>
	public void Dispose() {
		if (this.Value is IDisposable disposable) {
			disposable.Dispose();
		}
	}
}