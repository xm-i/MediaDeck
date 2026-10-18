namespace MediaDeck.Composition.Interfaces.Threading;

/// <summary>
/// UIスレッド上で処理を実行するためのディスパッチャーインターフェース。
/// </summary>
public interface IUiDispatcher {
	/// <summary>
	/// 指定されたアクションを UI スレッド上で同期的に実行します。
	/// </summary>
	/// <param name="action">実行するアクション</param>
	public void Run(Action action);

	/// <summary>
	/// 指定されたアクションを UI スレッド上で非同期に実行します。
	/// </summary>
	/// <param name="action">実行するアクション</param>
	/// <returns>非同期タスク</returns>
	public Task RunAsync(Action action);

	/// <summary>
	/// 指定された非同期アクションを UI スレッド上で非同期に実行します。
	/// </summary>
	/// <param name="action">実行する非同期アクション</param>
	/// <returns>非同期タスク</returns>
	public Task RunAsync(Func<Task> action);
}