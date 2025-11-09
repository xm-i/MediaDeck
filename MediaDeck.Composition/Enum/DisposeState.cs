namespace MediaDeck.Composition.Enum;
/// <summary>
/// Dispose状態
/// </summary>
public enum DisposeState {
	/// <summary>
	/// Disposeされていない
	/// </summary>
	NotDisposed,
	/// <summary>
	/// Dispose処理中
	/// </summary>
	Disposing,
	/// <summary>
	/// Dispose完了済み
	/// </summary>
	Disposed
}
