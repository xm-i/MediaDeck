namespace MediaDeck.Composition.Objects;

/// <summary>
/// 現在のDIスコープにおける通知のターゲットWindowIDを提供するクラス。
/// </summary>
[Inject(InjectServiceLifetime.Scoped)]
public class NotificationContextProvider {
	/// <summary>
	/// TargetWindowIdを遅延評価で動的に解決する関数
	/// </summary>
	public Func<Guid?>? TargetWindowIdResolver {
		get; set;
	}

	/// <summary>
	/// 現在のコンテキストにおけるTargetWindowIdを取得します。
	/// </summary>
	/// <returns>ターゲットとなるWindowId。グローバルの場合はnull。</returns>
	public Guid? GetTargetWindowId() {
		return this.TargetWindowIdResolver?.Invoke();
	}
}