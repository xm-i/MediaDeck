using System.Threading;

using MediaDeck.Common.Base;
using MediaDeck.Core.Stores.State;

namespace MediaDeck.Services;

/// <summary>
/// 状態保存を行うサービス実装クラス
/// </summary>
/// <remarks>
/// コンストラクタ
/// </remarks>
/// <param name="stateStore">状態管理ストア</param>
[Inject(InjectServiceLifetime.Singleton)]
public class SaveStateService(IStateStore stateStore) : ServiceBase {
	private readonly IStateStore _stateStore = stateStore;
	private readonly Lock _lock = new();
	private DateTime _lastSavedAt = DateTime.MinValue;

	/// <summary>
	/// 保存要求。前回の実行から30秒未満の場合は無視されます。
	/// </summary>
	public void RequestSave() {
		lock (this._lock) {
			// 前回の保存から30秒経過していない場合は何もしない
			if (DateTime.Now - this._lastSavedAt < TimeSpan.FromSeconds(30)) {
				return;
			}

			this._lastSavedAt = DateTime.Now;
		}

		this._stateStore.Save();
	}
}