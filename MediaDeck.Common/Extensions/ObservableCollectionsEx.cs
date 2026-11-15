using MediaDeck.Common.Collections;
using ObservableCollections;

namespace MediaDeck.Common.Extensions;

/// <summary>
/// <see cref="ObservableCollections"/> に対する拡張メソッドを提供します。
/// </summary>
public static class ObservableCollectionsEx {
	/// <summary>
	/// 要素が削除・置換・クリア・破棄された際に <see cref="IDisposable"/> を自動破棄する同期ビューを生成します。
	/// </summary>
	/// <typeparam name="TSource">ソース要素の型</typeparam>
	/// <typeparam name="TView">ビュー要素の型</typeparam>
	/// <param name="source">ソースコレクション</param>
	/// <param name="transform">変換関数</param>
	/// <returns>要素自動破棄つき同期ビュー</returns>
	public static ISynchronizedView<TSource, TView> CreateViewWithDisposal<TSource, TView>(
		this IObservableCollection<TSource> source,
		Func<TSource, TView> transform) {
		ArgumentNullException.ThrowIfNull(source);
		ArgumentNullException.ThrowIfNull(transform);
		var view = source.CreateView(transform);
		return new DisposingSynchronizedView<TSource, TView>(view);
	}

	/// <summary>
	/// 既存の <see cref="ISynchronizedView{T, TView}"/> に対して、要素が削除・置換・クリア・破棄された際に <see cref="IDisposable"/> を自動破棄するラッパーを適用します。
	/// </summary>
	/// <typeparam name="TSource">ソース要素の型</typeparam>
	/// <typeparam name="TView">ビュー要素の型</typeparam>
	/// <param name="view">対象の同期ビュー</param>
	/// <returns>要素自動破棄つき同期ビュー</returns>
	public static ISynchronizedView<TSource, TView> WithDisposal<TSource, TView>(
		this ISynchronizedView<TSource, TView> view) {
		ArgumentNullException.ThrowIfNull(view);
		if (view is DisposingSynchronizedView<TSource, TView>) {
			return view;
		}
		return new DisposingSynchronizedView<TSource, TView>(view);
	}

	/// <summary>
	/// 同期ビューから <see cref="INotifyCollectionChangedSynchronizedViewList{T}"/> を生成し、
	/// リストの破棄時に親ビュー（およびその要素）も連動して破棄するようにします。
	/// </summary>
	/// <typeparam name="TSource">ソース要素の型</typeparam>
	/// <typeparam name="TView">ビュー要素の型</typeparam>
	/// <param name="view">同期ビュー</param>
	/// <param name="dispatcher">イベントディスパッチャ</param>
	/// <returns>親ビュー連動破棄つき同期ビューリスト</returns>
	public static DisposingNotifyCollectionChangedSynchronizedViewList<TView> ToNotifyCollectionChangedWithDisposal<TSource, TView>(
		this ISynchronizedView<TSource, TView> view,
		ICollectionEventDispatcher dispatcher) {
		ArgumentNullException.ThrowIfNull(view);
		ArgumentNullException.ThrowIfNull(dispatcher);
		var inner = view.ToNotifyCollectionChanged(dispatcher);
		return new DisposingNotifyCollectionChangedSynchronizedViewList<TView>(inner, view);
	}

	/// <summary>
	/// 同期ビューから <see cref="DisposingNotifyCollectionChangedSynchronizedViewList{T}"/> を生成し、
	/// リストの破棄時に親ビュー（およびその要素）も連動して破棄するようにします。
	/// </summary>
	/// <typeparam name="TSource">ソース要素の型</typeparam>
	/// <typeparam name="TView">ビュー要素の型</typeparam>
	/// <param name="view">同期ビュー</param>
	/// <returns>親ビュー連動破棄つき同期ビューリスト</returns>
	public static DisposingNotifyCollectionChangedSynchronizedViewList<TView> ToNotifyCollectionChangedWithDisposal<TSource, TView>(
		this ISynchronizedView<TSource, TView> view) {
		ArgumentNullException.ThrowIfNull(view);
		var inner = view.ToNotifyCollectionChanged();
		return new DisposingNotifyCollectionChangedSynchronizedViewList<TView>(inner, view);
	}
}