using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using ObservableCollections;

namespace MediaDeck.Common.Collections;

/// <summary>
/// <see cref="INotifyCollectionChangedSynchronizedViewList{T}"/> と親の <see cref="IDisposable"/>（生成元ビュー等）を一括して破棄するラッパークラス。
/// </summary>
/// <typeparam name="TView">ビュー要素の型</typeparam>
public sealed class DisposingNotifyCollectionChangedSynchronizedViewList<TView> : INotifyCollectionChangedSynchronizedViewList<TView> {
	private readonly NotifyCollectionChangedSynchronizedViewList<TView> _inner;
	private readonly IDisposable _parentView;
	private bool _isDisposed;

	/// <summary>
	/// <see cref="DisposingNotifyCollectionChangedSynchronizedViewList{TView}"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	/// <param name="inner">同期ビューリスト本体</param>
	/// <param name="parentView">親ビュー</param>
	public DisposingNotifyCollectionChangedSynchronizedViewList(NotifyCollectionChangedSynchronizedViewList<TView> inner, IDisposable parentView) {
		ArgumentNullException.ThrowIfNull(inner);
		ArgumentNullException.ThrowIfNull(parentView);
		this._inner = inner;
		this._parentView = parentView;
	}

	/// <inheritdoc/>
	public TView this[int index] {
		get {
			return this._inner[index];
		}

		set {
			this._inner[index] = value;
		}
	}

	/// <inheritdoc/>
	object? IList.this[int index] {
		get {
			return ((IList)this._inner)[index];
		}

		set {
			((IList)this._inner)[index] = value;
		}
	}

	/// <inheritdoc/>
	public int Count {
		get {
			return this._inner.Count;
		}
	}

	/// <inheritdoc/>
	public bool IsReadOnly {
		get {
			return this._inner.IsReadOnly;
		}
	}

	/// <inheritdoc/>
	public bool IsFixedSize {
		get {
			return ((IList)this._inner).IsFixedSize;
		}
	}

	/// <inheritdoc/>
	public bool IsSynchronized {
		get {
			return ((ICollection)this._inner).IsSynchronized;
		}
	}

	/// <inheritdoc/>
	public object SyncRoot {
		get {
			return ((ICollection)this._inner).SyncRoot;
		}
	}

	/// <inheritdoc/>
	public event NotifyCollectionChangedEventHandler? CollectionChanged {
		add {
			this._inner.CollectionChanged += value;
		}

		remove {
			this._inner.CollectionChanged -= value;
		}
	}

	/// <inheritdoc/>
	public event PropertyChangedEventHandler? PropertyChanged {
		add {
			this._inner.PropertyChanged += value;
		}

		remove {
			this._inner.PropertyChanged -= value;
		}
	}

	/// <inheritdoc/>
	public void Add(TView item) {
		this._inner.Add(item);
	}

	/// <inheritdoc/>
	public int Add(object? value) {
		return ((IList)this._inner).Add(value);
	}

	/// <inheritdoc/>
	public void Clear() {
		this._inner.Clear();
	}

	/// <inheritdoc/>
	public bool Contains(TView item) {
		return this._inner.Contains(item);
	}

	/// <inheritdoc/>
	public bool Contains(object? value) {
		return ((IList)this._inner).Contains(value);
	}

	/// <inheritdoc/>
	public void CopyTo(TView[] array, int arrayIndex) {
		((ICollection<TView>)this._inner).CopyTo(array, arrayIndex);
	}

	/// <inheritdoc/>
	public void CopyTo(Array array, int index) {
		((ICollection)this._inner).CopyTo(array, index);
	}

	/// <inheritdoc/>
	public IEnumerator<TView> GetEnumerator() {
		return this._inner.GetEnumerator();
	}

	/// <inheritdoc/>
	IEnumerator IEnumerable.GetEnumerator() {
		return ((IEnumerable)this._inner).GetEnumerator();
	}

	/// <inheritdoc/>
	public int IndexOf(TView item) {
		return this._inner.IndexOf(item);
	}

	/// <inheritdoc/>
	public int IndexOf(object? value) {
		return ((IList)this._inner).IndexOf(value);
	}

	/// <inheritdoc/>
	public void Insert(int index, TView item) {
		this._inner.Insert(index, item);
	}

	/// <inheritdoc/>
	public void Insert(int index, object? value) {
		((IList)this._inner).Insert(index, value);
	}

	/// <inheritdoc/>
	public bool Remove(TView item) {
		return this._inner.Remove(item);
	}

	/// <inheritdoc/>
	public void Remove(object? value) {
		((IList)this._inner).Remove(value);
	}

	/// <inheritdoc/>
	public void RemoveAt(int index) {
		this._inner.RemoveAt(index);
	}

	/// <inheritdoc/>
	public void Dispose() {
		if (this._isDisposed) {
			return;
		}
		this._isDisposed = true;
		this._inner.Dispose();
		this._parentView.Dispose();
	}
}