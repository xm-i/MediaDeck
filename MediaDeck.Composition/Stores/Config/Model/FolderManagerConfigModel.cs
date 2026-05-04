using MediaDeck.Composition.Stores.Config.Model.Objects;

using R3.JsonConfig.Attributes;

namespace MediaDeck.Composition.Stores.Config.Model;

/// <summary>
/// フォルダ管理設定
/// </summary>
[Inject(InjectServiceLifetime.Singleton)]
[GenerateR3JsonConfigDto]
public class FolderManagerConfigModel {
	/// <summary>
	/// 管理対象フォルダリスト
	/// </summary>
	public ObservableList<FolderModel> Folders {
		get;
	} = [];
}