using MediaDeck.Composition.Enum;
using R3.JsonConfig.Attributes;

namespace MediaDeck.Composition.Stores.State.Model;

/// <summary>
/// アプリケーション全体で共有される状態（Singleton）
/// </summary>
[Inject(InjectServiceLifetime.Singleton)]
[GenerateR3JsonConfigDto]
public class AppStateModel(FolderManagerStateModel folderManagerState, SearchDefinitionsStateModel searchDefinitions, SearchStateModel defaultTabSearchState, ViewerStateModel defaultTabViewerState) {
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
	/// 新規タブのデフォルト検索状態
	/// </summary>
	public SearchStateModel DefaultTabSearchState {
		get;
		set;
	} = defaultTabSearchState;

	/// <summary>
	/// 新規タブのデフォルトビューアー状態
	/// </summary>
	public ViewerStateModel DefaultTabViewerState {
		get;
		set;
	} = defaultTabViewerState;
}