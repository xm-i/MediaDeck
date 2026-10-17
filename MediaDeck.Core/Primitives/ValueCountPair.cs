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

	public bool Equals(ValueCountPair<T> other) {
		if (EqualityComparer<T>.Default.Equals(this.Value, other.Value) && this.Count == other.Count) {
			return true;
		}
		return false;
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