namespace MediaDeck.Composition.Interfaces.Services;

/// <summary>
/// アプリケーションの実行環境情報を提供するサービスインターフェース
/// </summary>
public interface IAppEnvironmentService {
	/// <summary>
	/// Unpackaged版（非Microsoft Store配布版）として動作しているかどうかを取得します。
	/// </summary>
	public bool IsUnpackaged {
		get;
	}

	/// <summary>
	/// 現在のアプリケーションバージョンを取得します。
	/// </summary>
	public Version CurrentVersion {
		get;
	}
}