using Microsoft.UI.Dispatching;

namespace MediaDeck.Composition.Interfaces;

/// <summary>
/// UIスレッドでの実行を抽象化するインターフェース
/// </summary>
public interface IDispatcherGate {
	/// <summary>
	/// DispatcherQueueを初期化します。
	/// </summary>
	/// <param name="dispatcherQueue">UIスレッドのDispatcherQueue</param>
	public void Initialize(DispatcherQueue dispatcherQueue);

	/// <summary>
	/// 指定されたアクションをUIスレッドで実行します。
	/// </summary>
	/// <param name="action">実行するアクション</param>
	public void BeginInvoke(Action action);

	/// <summary>
	/// 現在のスレッドがUIスレッドかどうかを取得します。
	/// </summary>
	public bool HasThreadAccess {
		get;
	}
}
