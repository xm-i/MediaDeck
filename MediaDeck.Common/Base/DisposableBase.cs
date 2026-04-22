using System.Collections.Concurrent;
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
#if DEBUG
	private readonly InstanceTrackingInfo _trackingInfo;
	public static readonly ConcurrentDictionary<Type, ConcurrentBag<InstanceTrackingInfo>> CreatedInstances = [];
	public static IEnumerable<InstanceTrackingInfo> SortedCreatedInstances {
		get {
			return CreatedInstances.Values.SelectMany(bag => bag).OrderBy(info => info.CreationOrder);
		}
	}
#endif
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

	protected DisposableBase() {
#if DEBUG
		this._trackingInfo = new InstanceTrackingInfo(this);
		var bag = CreatedInstances.GetOrAdd(this.GetType(), _ => []);
		bag.Add(this._trackingInfo);
#endif
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
#if DEBUG
				this._trackingInfo.RecordDispose();
#endif
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
		this._trackingInfo.RecordFinalize();
		if (this.DisposeState == DisposeState.NotDisposed) {
			Debug.WriteLine($"[DISPOSE LEAK] {this.GetType().FullName} がDisposeされずにGCされました。");
		}
	}
#endif
}

#if DEBUG
/// <summary>
/// インスタンスの生成、破棄、GCの情報を記録するクラス。
/// </summary>
public class InstanceTrackingInfo {
	private static long _globalEventOrder = 0;

	/// <summary>対象インスタンスへの弱参照</summary>
	public WeakReference<DisposableBase> Instance {
		get;
	}

	public Type Type {
		get;
	}

	/// <summary>生成順序</summary>
	public long CreationOrder {
		get;
	}

	/// <summary>生成時刻</summary>
	public DateTime CreatedAt {
		get;
	}

	/// <summary>Dispose順序</summary>
	public long? DisposeOrder {
		get; private set;
	}

	/// <summary>Dispose時刻</summary>
	public DateTime? DisposedAt {
		get; private set;
	}

	/// <summary>ファイナライズ（GC）順序</summary>
	public long? FinalizeOrder {
		get; private set;
	}

	/// <summary>ファイナライズ（GC）時刻</summary>
	public DateTime? FinalizedAt {
		get; private set;
	}

	public string? Status {
		get;
		set;
	}

	public InstanceTrackingInfo(DisposableBase instance) {
		this.Instance = new WeakReference<DisposableBase>(instance);
		this.Type = instance.GetType();
		this.CreatedAt = DateTime.Now;
		this.CreationOrder = Interlocked.Increment(ref _globalEventOrder);
		this.Status = null;
	}

	public void RecordDispose() {
		this.DisposedAt = DateTime.Now;
		this.DisposeOrder = Interlocked.Increment(ref _globalEventOrder);
		this.Status += "Disposed";
	}

	public void RecordFinalize() {
		this.FinalizedAt = DateTime.Now;
		this.FinalizeOrder = Interlocked.Increment(ref _globalEventOrder);
		this.Status += "Finalized";
	}

	public override string ToString() {
		var instanceAlive = this.Instance.TryGetTarget(out var target) && target != null;
		return $"[{this.Type.Name,-32}] {this.Status ?? "Created",-20} [{this.CreationOrder,-10}] {this.CreatedAt:HH:mm:ss.ffff}";
	}
}
#endif
