using R3.JsonConfig.Attributes;

namespace MediaDeck.Composition.Stores.State.Model;

/// <summary>
/// アプリケーション全体で共有される状態（Singleton）
/// </summary>
[Inject(InjectServiceLifetime.Singleton)]
[GenerateR3JsonConfigDto]
public class AppStateModel(FolderManagerStateModel folderManagerState, SearchDefinitionsStateModel searchDefinitions) {
	public FolderManagerStateModel FolderManagerState {
		get;
		set;
	} = folderManagerState;

	public SearchDefinitionsStateModel SearchDefinitions {
		get;
		set;
	} = searchDefinitions;
}