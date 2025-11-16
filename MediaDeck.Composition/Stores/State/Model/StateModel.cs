using R3.JsonConfig.Attributes;

namespace MediaDeck.Composition.Stores.State.Model;
/// <summary>
/// 検索状態
/// </summary>

[AddSingleton]
[GenerateR3JsonConfigDefaultDto]
public class StateModel(SearchStateModel searchState, FolderManagerStateModel folderManagerState) {

	public SearchStateModel SearchState {
		get;
		set;
	} = searchState;

	public FolderManagerStateModel FolderManagerState {
		get;
		set;
	} = folderManagerState;
}