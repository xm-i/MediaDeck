namespace MediaDeck.Composition.Objects;

/// <summary>
/// リソースへのアクセス管理に使用するロックを表し、複数のスレッドによる読み取りや排他アクセスでの書き込みを実現します。
/// ロック時に返却されるオブジェクトを破棄することで、ロックの解除が行えます。
/// </summary>
public sealed class DisposableLock : IDisposable {
	private readonly ReaderWriterLockSlim _lock;
	private bool _disposed;

	/// <summary>
	/// コンストラクタ
	/// </summary>
	/// <param name="recursionPolicy">同じスレッドが複数回ロックに入ることができるかどうかを指定します。</param>
	public DisposableLock(LockRecursionPolicy recursionPolicy) {
		this._lock = new ReaderWriterLockSlim(recursionPolicy);
	}

	/// <summary>
	/// 読み取りモードでロックに入ることを試みます。
	/// </summary>
	/// <returns>ロック解除用の <see cref="IDisposable"/>。</returns>
	public IDisposable DisposableEnterReadLock() {
		if (this._disposed) {
			return new DisposeObject(null);
		}
		this._lock.EnterReadLock();
		return new DisposeObject(this._lock.ExitReadLock);
	}

	/// <summary>
	/// 書き込みモードでロックに入ることを試みます。
	/// </summary>
	/// <returns>ロック解除用の <see cref="IDisposable"/>。</returns>
	public IDisposable DisposableEnterWriteLock() {
		if (this._disposed) {
			return new DisposeObject(null);
		}
		this._lock.EnterWriteLock();
		return new DisposeObject(this._lock.ExitWriteLock);
	}

	/// <summary>
	/// 内部のロックリソースを破棄します。
	/// </summary>
	public void Dispose() {
		if (this._disposed) {
			return;
		}
		this._disposed = true;
		this._lock.Dispose();
	}

	/// <summary>
	/// ロック解除用オブジェクト
	/// </summary>
	private sealed class DisposeObject : IDisposable {
		private Action? _disposeAction;

		/// <summary>
		/// コンストラクタ
		/// </summary>
		/// <param name="disposeAction">Dispose時のアクション(ロック解除)</param>
		public DisposeObject(Action? disposeAction) {
			this._disposeAction = disposeAction;
		}

		/// <summary>
		/// Dispose
		/// </summary>
		public void Dispose() {
			Interlocked.Exchange(ref this._disposeAction, null)?.Invoke();
		}
	}
}