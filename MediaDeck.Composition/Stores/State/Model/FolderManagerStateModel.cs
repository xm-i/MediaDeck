using MediaDeck.Composition.Stores.State.Model.Objects;

using R3.JsonConfig.Attributes;

namespace MediaDeck.Composition.Stores.State.Model;

/// <summary>
/// フォルダ管理状態
/// </summary>

[Inject(InjectServiceLifetime.Singleton)]
[GenerateR3JsonConfigDto]
public class FolderManagerStateModel {
	/// <summary>
	/// 管理対象フォルダリスト
	/// </summary>
	public ObservableList<FolderModel> Folders {
		get;
	} = [];
}
