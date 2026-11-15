using System.Reflection;
using MediaDeck.Composition.Interfaces.Services;

namespace MediaDeck.Services;

/// <summary>
/// アプリケーションの実行環境情報を提供するサービス
/// </summary>
[Inject(InjectServiceLifetime.Singleton, typeof(IAppEnvironmentService))]
public class AppEnvironmentService : IAppEnvironmentService {
	/// <summary>
	/// <see cref="AppEnvironmentService"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	public AppEnvironmentService() {
		this.CurrentVersion = Assembly.GetEntryAssembly()?.GetName().Version ?? new Version(1, 0, 0, 0);
	}

	/// <inheritdoc />
	public bool IsUnpackaged {
		get {
#if UNPACKAGED
			return true;
#else
			return false;
#endif
		}
	}

	/// <inheritdoc />
	public Version CurrentVersion {
		get;
	}
}