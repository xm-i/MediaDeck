using MediaDeck.Composition.Interfaces.Files;

namespace MediaDeck.Composition.Interfaces.Notifications;

/// <summary>
/// 検索条件変更の中央通知ハブ。
/// </summary>
public interface ISearchConditionNotificationDispatcher : IDisposable {
	/// <summary>検索条件の追加リクエスト</summary>
	public Subject<ISearchCondition> AddRequest {
		get;
	}

	/// <summary>検索条件の削除リクエスト</summary>
	public Subject<ISearchCondition> RemoveRequest {
		get;
	}

	/// <summary>検索条件リストの更新リクエスト</summary>
	public Subject<Action<ObservableList<ISearchCondition>>> UpdateRequest {
		get;
	}

	/// <summary>ソート条件変更通知</summary>
	public Subject<Unit> SortChanged {
		get;
	}

	/// <summary>フィルター条件変更通知</summary>
	public Subject<Unit> FilterChanged {
		get;
	}

	/// <summary>手動リロードリクエスト（即時発火）</summary>
	public Subject<Unit> ReloadRequested {
		get;
	}

	/// <summary>
	/// ソート・フィルター・検索ワードのすべての変更通知を統合したストリーム。
	/// </summary>
	public Observable<Unit> SearchRequested {
		get;
	}
}