using MediaDeck.Composition.Interfaces.Files;

namespace MediaDeck.Core.Models.NotificationDispatcher;

/// <summary>
/// 検索条件変更の中央通知ハブ。
/// ソート・フィルター・検索ワード（トークン）など、すべての検索トリガーを集約し、
/// 外部クラスは <see cref="SearchRequested"/> を監視するだけで全条件変更を受信できる。
/// </summary>
[Inject(InjectServiceLifetime.Scoped)]
public class SearchConditionNotificationDispatcher : IDisposable {
	/// <summary>コンストラクタ</summary>
	public SearchConditionNotificationDispatcher() {
		// 検索ワード（トークン）の追加・削除・更新はユーザーのキーボード操作で高頻度に変化しうるため
		// Debounce を挟んで無駄な検索発火を抑制する。
		var searchConditionChanged = Observable.Merge(
			this.AddRequest.Select(_ => Unit.Default),
			this.RemoveRequest.Select(_ => Unit.Default),
			this.UpdateRequest.Select(_ => Unit.Default))
			.Debounce(TimeSpan.FromMilliseconds(300));

		// ソート・フィルターは即時
		var sortOrFilterChanged = Observable.Merge(
			this.SortChanged,
			this.FilterChanged);

		// 全条件変更を 1 本のストリームに統合して公開
		this.SearchRequested = Observable.Merge(
			searchConditionChanged,
			sortOrFilterChanged,
			this.ReloadRequested)
			.Publish()
			.RefCount();
	}

	// ─── 検索ワード（トークン）系 ───────────────────────────────────────────────

	/// <summary>検索条件の追加リクエスト</summary>
	public Subject<ISearchCondition> AddRequest { get; } = new();

	/// <summary>検索条件の削除リクエスト</summary>
	public Subject<ISearchCondition> RemoveRequest { get; } = new();

	/// <summary>検索条件リストの更新リクエスト</summary>
	public Subject<Action<ObservableList<ISearchCondition>>> UpdateRequest { get; } = new();

	// ─── ソート・フィルター系 ──────────────────────────────────────────────────

	/// <summary>ソート条件変更通知</summary>
	public Subject<Unit> SortChanged { get; } = new();

	/// <summary>フィルター条件変更通知</summary>
	public Subject<Unit> FilterChanged { get; } = new();

	/// <summary>手動リロードリクエスト（即時発火）</summary>
	public Subject<Unit> ReloadRequested { get; } = new();

	// ─── 統合ストリーム ────────────────────────────────────────────────────────

	/// <summary>
	/// ソート・フィルター・検索ワードのすべての変更通知を統合したストリーム。
	/// 検索ワード変更には内部で Debounce が適用済み。
	/// 外部クラスはこのストリームを監視するだけでよい。
	/// </summary>
	public Observable<Unit> SearchRequested {
		get;
	}

	/// <inheritdoc/>
	public void Dispose() {
		this.AddRequest.Dispose();
		this.RemoveRequest.Dispose();
		this.UpdateRequest.Dispose();
		this.SortChanged.Dispose();
		this.FilterChanged.Dispose();
		this.ReloadRequested.Dispose();
	}
}