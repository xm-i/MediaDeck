using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Objects;
using System.Threading;
using System;
using CommunityToolkit.Mvvm.ComponentModel;
using R3;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.ComponentModel;
using System.Collections.Concurrent;

namespace MediaDeck.Composition.Bases;
public class ModelBase : ObservableObject, IModelBase {
	/// <summary>
	/// Dispose用Lockオブジェクト
	/// 処理を行っている途中でDisposeされるとマズイ場合、このオブジェクトでロックしておく。
	/// </summary>
	protected readonly DisposableLock DisposeLock = new(LockRecursionPolicy.SupportsRecursion);
	/// <summary>
	/// まとめてDispose
	/// </summary>
	private CompositeDisposable? _compositeDisposable;

	/// <summary>
	/// Dispose通知用Subject
	/// </summary>
	private readonly Subject<Unit> _onDisposed = new();

	/// <summary>
	/// バッキングフィールド
	/// </summary>
	private readonly ConcurrentDictionary<string, object?> _backingFields = new();

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
	/// バッキングフィールドから値を取得(Boxingが発生するのでパフォーマンスが重要な場面では使わない)
	/// </summary>
	/// <typeparam name="T">型</typeparam>
	/// <param name="member">メンバー名</param>
	/// <returns>値</returns>
	protected T? GetValue<T>([CallerMemberName] string member = "") {
		return this.GetValue<T?>(() => default, member);
	}

	/// <summary>
	/// バッキングフィールドから値を取得(Boxingが発生するのでパフォーマンスが重要な場面では使わない)
	/// </summary>
	/// <typeparam name="T">型</typeparam>
	/// <param name="valueFactory">バッキングフィールドに値がなかった場合の値生成関数</param>
	/// <param name="member">メンバー名</param>
	/// <returns>値</returns>
	protected T? GetValue<T>(Func<T?> valueFactory, [CallerMemberName] string member = "") {
		return
			(T?)this
				._backingFields
				.GetOrAdd(member, _ => valueFactory());
	}

	/// <summary>
	/// バッキングフィールドに値を設定(Boxingが発生するのでパフォーマンスが重要な場面では使わない)
	/// </summary>
	/// <typeparam name="T">型</typeparam>
	/// <param name="value">値</param>
	/// <param name="member">メンバー名</param>
	protected void SetValue<T>(T value, [CallerMemberName] string member = "") {
		if (EqualityComparer<T>.Default.Equals(this.GetValue<T>(member), value)) {
			return;
		}
		this._backingFields[member] = value;
		this.OnPropertyChanged(member);
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
}
