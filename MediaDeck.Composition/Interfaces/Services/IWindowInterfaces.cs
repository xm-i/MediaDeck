namespace MediaDeck.Composition.Interfaces.Services;

/// <summary>
/// 個々のウィンドウに対応するコンテキスト。
/// ウィンドウ固有のDIスコープとリソースを管理する。
/// </summary>
public interface IWindowContext : IDisposable {
	/// <summary>
	/// ウィンドウ固有のサービスプロバイダー
	/// </summary>
	public IServiceProvider Services {
		get;
	}

	/// <summary>
	/// ウィンドウの一意識別子
	/// </summary>
	public Guid WindowId {
		get;
	}
}