using MediaDeck.Composition.Enum;

namespace MediaDeck.Composition.Interfaces;

/// <summary>
/// Dispose可能なオブジェクトの基底インターフェース
/// </summary>
public interface IDisposableBase : IDisposable {
	/// <summary>
	/// Dispose済みか
	/// </summary>
	public DisposeState DisposeState {
		get;
	}

	/// <summary>
	/// Dispose通知
	/// </summary>
	public Observable<Unit> OnDisposed {
		get;
	}

	/// <summary>
	/// まとめてDispose
	/// </summary>
	public CompositeDisposable CompositeDisposable {
		get;
	}
}