using GenJsonConfig.Attributes;

namespace MediaDeck.Composition.Stores.State.Model;

/// <summary>
/// アプリケーション全体で共有される状態（Singleton）
/// </summary>
/// <param name="defaultTabState">新規タブのデフォルト状態</param>
[Inject(InjectServiceLifetime.Singleton)]
[GenerateJsonConfigDto]
public class AppStateModel(DefaultTabStateModel defaultTabState) {

	/// <summary>
	/// 新規タブのデフォルト状態
	/// </summary>
	public DefaultTabStateModel DefaultTabState {
		get;
		set;
	} = defaultTabState;
}