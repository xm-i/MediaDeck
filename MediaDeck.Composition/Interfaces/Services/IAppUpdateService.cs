using MediaDeck.Composition.Objects;

namespace MediaDeck.Composition.Interfaces.Services;

/// <summary>
/// アプリケーションの新バージョン確認を行うサービスインターフェース
/// </summary>
public interface IAppUpdateService {
	/// <summary>
	/// 利用可能な新しいバージョンの情報（存在しない、または未確認の場合は null）
	/// </summary>
	public ReadOnlyReactiveProperty<AppUpdateInfo?> AvailableUpdate {
		get;
	}

	/// <summary>
	/// 更新確認を実行中かどうか
	/// </summary>
	public ReadOnlyReactiveProperty<bool> IsChecking {
		get;
	}

	/// <summary>
	/// 最新バージョンの確認を実行します。
	/// </summary>
	/// <param name="cancellationToken">キャンセルトークン</param>
	/// <returns>更新確認の結果</returns>
	public Task<UpdateCheckResult> CheckForUpdatesAsync(CancellationToken cancellationToken = default);
}