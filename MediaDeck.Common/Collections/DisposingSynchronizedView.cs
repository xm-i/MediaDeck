using System.Collections;
using System.Collections.Specialized;
using ObservableCollections;

namespace MediaDeck.Common.Collections;

/// <summary>
/// 要素がコレクションから削除、置換、クリアされた際、およびビュー自身が破棄された際に、
/// 生成された要素 ViewModel などの <see cref="IDisposable"/> リソースを自動的に破棄する <see cref="ISynchronizedView{T, TView}"/> のラッパークラス。
/// </summary>
/// <typeparam name="TSource">ソース要素の型</typeparam>
/// <typeparam name="TView">変換後ビュー要素の型</typeparam>
public sealed class DisposingSynchronizedView<TSource, TView> : ISynchronizedView<TSource, TView> {
	private readonly ISynchronizedView<TSource, TView> _inner;
	private readonly List<TView> _activeViews = [];
	private readonly object _gate = new();
	private bool _isDisposed;

	/// <summary>
	/// <see cref="DisposingSynchronizedView{TSource, TView}"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	/// <param name="inner">ラップ対象の <see cref="ISynchronizedView{T, TView}"/></param>
	public DisposingSynchronizedView(ISynchronizedView<TSource, TView> inner) {
		ArgumentNullException.ThrowIfNull(inner);
		this._inner = inner;

		lock (this._gate) {
			foreach (var item in inner.Unfiltered) {
				this._activeViews.Add(item.View);
			}
		}

		this._inner.ViewChanged += this.OnViewChanged;
	}

	private void OnViewChanged(in SynchronizedViewChangedEventArgs<TSource, TView> e) {
		switch (e.Action) {
			case NotifyCollectionChangedAction.Add:
				lock (this._gate) {
					if (this._isDisposed) {
						return;
					}
					if (e.IsSingleItem) {
						this._activeViews.Add(e.NewItem.View);
					} else {
						foreach (var item in e.NewViews) {
							this._activeViews.Add(item);
						}
					}
				}
				break;

			case NotifyCollectionChangedAction.Remove: {
					List<TView> toDispose = [];
					lock (this._gate) {
						if (this._isDisposed) {
							return;
						}
						if (e.IsSingleItem) {
							this._activeViews.Remove(e.OldItem.View);
							toDispose.Add(e.OldItem.View);
						} else {
							foreach (var item in e.OldViews) {
								this._activeViews.Remove(item);
								toDispose.Add(item);
							}
						}
					}
					foreach (var item in toDispose) {
						(item as IDisposable)?.Dispose();
					}
					break;
				}

			case NotifyCollectionChangedAction.Replace: {
					List<TView> toDispose = [];
					lock (this._gate) {
						if (this._isDisposed) {
							return;
						}
						if (e.IsSingleItem) {
							this._activeViews.Remove(e.OldItem.View);
							toDispose.Add(e.OldItem.View);
							this._activeViews.Add(e.NewItem.View);
						} else {
							foreach (var item in e.OldViews) {
								this._activeViews.Remove(item);
								toDispose.Add(item);
							}
							foreach (var item in e.NewViews) {
								this._activeViews.Add(item);
							}
						}
					}
					foreach (var item in toDispose) {
						(item as IDisposable)?.Dispose();
					}
					break;
				}

			case NotifyCollectionChangedAction.Reset: {
					List<TView> toDispose;
					lock (this._gate) {
						if (this._isDisposed) {
							return;
						}
						toDispose = [.. this._activeViews];
						this._activeViews.Clear();
					}
					foreach (var item in toDispose) {
						(item as IDisposable)?.Dispose();
					}
					break;
				}

			case NotifyCollectionChangedAction.Move:
				// 移動時は要素の破棄・生成は行わない
				break;
		}
	}

	/// <inheritdoc/>
	public object SyncRoot {
		get {
			return this._inner.SyncRoot;
		}
	}

	/// <inheritdoc/>
	public ISynchronizedViewFilter<TSource, TView> Filter {
		get {
			return this._inner.Filter;
		}
	}

	/// <inheritdoc/>
	public IEnumerable<(TSource, TView)> Filtered {
		get {
			return this._inner.Filtered;
		}
	}

	/// <inheritdoc/>
	public IEnumerable<(TSource, TView)> Unfiltered {
		get {
			return this._inner.Unfiltered;
		}
	}

	/// <inheritdoc/>
	public int UnfilteredCount {
		get {
			return this._inner.UnfilteredCount;
		}
	}

	/// <inheritdoc/>
	public int Count {
		get {
			return this._inner.Count;
		}
	}

	/// <inheritdoc/>
	public event NotifyViewChangedEventHandler<TSource, TView>? ViewChanged {
		add {
			this._inner.ViewChanged += value;
		}

		remove {
			this._inner.ViewChanged -= value;
		}
	}

	/// <inheritdoc/>
	public event Action<RejectedViewChangedAction, int, int>? RejectedViewChanged {
		add {
			this._inner.RejectedViewChanged += value;
		}

		remove {
			this._inner.RejectedViewChanged -= value;
		}
	}

	/// <inheritdoc/>
	public event Action<NotifyCollectionChangedAction>? CollectionStateChanged {
		add {
			this._inner.CollectionStateChanged += value;
		}

		remove {
			this._inner.CollectionStateChanged -= value;
		}
	}

	/// <inheritdoc/>
	public void AttachFilter(ISynchronizedViewFilter<TSource, TView> filter) {
		this._inner.AttachFilter(filter);
	}

	/// <inheritdoc/>
	public void ResetFilter() {
		this._inner.ResetFilter();
	}

	/// <inheritdoc/>
	public ISynchronizedViewList<TView> ToViewList() {
		return this._inner.ToViewList();
	}

	/// <inheritdoc/>
	public NotifyCollectionChangedSynchronizedViewList<TView> ToNotifyCollectionChanged() {
		return this._inner.ToNotifyCollectionChanged();
	}

	/// <inheritdoc/>
	public NotifyCollectionChangedSynchronizedViewList<TView> ToNotifyCollectionChanged(ICollectionEventDispatcher? dispatcher) {
		return this._inner.ToNotifyCollectionChanged(dispatcher);
	}

	/// <inheritdoc/>
	public IEnumerator<TView> GetEnumerator() {
		return this._inner.GetEnumerator();
	}

	/// <inheritdoc/>
	IEnumerator IEnumerable.GetEnumerator() {
		return this._inner.GetEnumerator();
	}

	/// <inheritdoc/>
	public void Dispose() {
		List<TView> toDispose;
		lock (this._gate) {
			if (this._isDisposed) {
				return;
			}
			this._isDisposed = true;
			toDispose = [.. this._activeViews];
			this._activeViews.Clear();
		}

		this._inner.ViewChanged -= this.OnViewChanged;
		this._inner.Dispose();

		foreach (var item in toDispose) {
			(item as IDisposable)?.Dispose();
		}
	}
}