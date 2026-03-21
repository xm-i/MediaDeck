using MediaDeck.Composition.Interfaces;

using Microsoft.UI.Dispatching;

namespace MediaDeck.Composition.Utils;

/// <summary>
/// UIスレッドでの実行を抽象化する実装クラス
/// </summary>
[Inject(InjectServiceLifetime.Singleton, typeof(IDispatcherGate))]
public class DispatcherGate : IDispatcherGate {
	private DispatcherQueue? _dispatcherQueue;

	/// <inheritdoc />
	public void Initialize(DispatcherQueue dispatcherQueue) {
		this._dispatcherQueue = dispatcherQueue;
	}

	/// <inheritdoc />
	public bool HasThreadAccess {
		get {
			return this._dispatcherQueue?.HasThreadAccess ?? true;
		}
	}

	/// <inheritdoc />
	public void BeginInvoke(Action action) {
		if (this._dispatcherQueue == null || this.HasThreadAccess) {
			action();
		} else {
			this._dispatcherQueue.TryEnqueue(() => action());
		}
	}
}
