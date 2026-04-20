using System.Diagnostics;

using CommunityToolkit.Mvvm.ComponentModel;

using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces;
using MediaDeck.Composition.Objects;

namespace MediaDeck.Common.Base;

/// <summary>
/// Dispose可能なオブジェクトの基底クラス。
/// CompositeDisposableによる一括破棄、DisposeLockによるスレッドセーフなDispose、
/// OnDisposedによるDispose通知を提供する。
/// </summary>
public class DisposableBase : ObservableObject, IDisposableBase {
	/// <summary>
	/// Dispose用Lockオブジェクト
	/// 処理を行っている途中でDisposeされるとマズイ場合、このオブジェクトでロックしておく。
	/// </summary>
	protected readonly DisposableLock DisposeLock = new(LockRecursionPolicy.SupportsRecursion);

	/// <summary>
	/// Dispose通知用Subject
	/// </summary>
	private readonly Subject<Unit> _onDisposed = new();

	/// <summary>
	/// まとめてDispose
	/// </summary>
	private CompositeDisposable? _compositeDisposable;

	/// <summary>
	/// Dispose済みか
	/// </summary>
	public DisposeState DisposeState {
		get;
		private set;
	}

	/// <summary>
	/// Dispose通知
	/// </summary>
	public Observable<Unit> OnDisposed {
		get {
			return this._onDisposed.AsObservable();
		}
	}

	/// <summary>
	/// まとめてDispose
	/// </summary>
	public CompositeDisposable CompositeDisposable {
		get {
			return this._compositeDisposable ??= [];
		}
	}

	/// <summary>
	/// Dispose
	/// </summary>
	public void Dispose() {
		this.Dispose(true);
		GC.SuppressFinalize(this);
	}

	/// <summary>
	/// Dispose
	/// </summary>
	/// <param name="disposing">マネージドリソースの破棄を行うかどうか</param>
	protected virtual void Dispose(bool disposing) {
		lock (this.DisposeLock) {
			if (this.DisposeState != DisposeState.NotDisposed) {
				return;
			}
			using (this.DisposeLock.DisposableEnterWriteLock()) {
				if (this.DisposeState != DisposeState.NotDisposed) {
					return;
				}
				this.DisposeState = DisposeState.Disposing;
			}
			if (disposing) {
				this._onDisposed.OnNext(Unit.Default);
				this._compositeDisposable?.Dispose();
			}
			using (this.DisposeLock.DisposableEnterWriteLock()) {
				this.DisposeState = DisposeState.Disposed;
			}
			this.DisposeLock.Dispose();
		}
	}

#if DEBUG
	/// <summary>
	/// デバッグ用ファイナライザ。Dispose漏れを検出する。
	/// </summary>
	~DisposableBase() {
		if (this.DisposeState == DisposeState.NotDisposed) {
			Debug.WriteLine($"[DISPOSE LEAK] {this.GetType().FullName} がDisposeされずにGCされました。");
		}
	}
#endif
}