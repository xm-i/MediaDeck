using MediaDeck.Composition.Enum;
using R3.JsonConfig.Attributes;

namespace MediaDeck.Composition.Stores.State.Model;

/// <summary>
/// アプリケーション全体で共有される状態（Singleton）
/// </summary>
[Inject(InjectServiceLifetime.Singleton)]
[GenerateR3JsonConfigDto]
public class AppStateModel(FolderManagerStateModel folderManagerState, SearchDefinitionsStateModel searchDefinitions, DefaultTabStateModel defaultTabState) {
	public FolderManagerStateModel FolderManagerState {
		get;
		set;
	} = folderManagerState;

	public SearchDefinitionsStateModel SearchDefinitions {
		get;
		set;
	} = searchDefinitions;

	/// <summary>
	/// アプリケーションのテーマ
	/// </summary>
	public ReactiveProperty<AppTheme> Theme {
		get;
		set;
	} = new(AppTheme.Default);

	/// <summary>
	/// 新規タブのデフォルト状態
	/// </summary>
	public DefaultTabStateModel DefaultTabState {
		get;
		set;
	} = defaultTabState;
}

