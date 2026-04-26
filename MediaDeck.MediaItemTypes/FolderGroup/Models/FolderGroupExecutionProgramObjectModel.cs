using MediaDeck.Composition.Enum;
using MediaDeck.MediaItemTypes.Base.Models;

using R3.JsonConfig.Attributes;

namespace MediaDeck.MediaItemTypes.FolderGroup.Models;

/// <summary>
/// フォルダグループ固有の実行設定（内部/外部の切り替えを含む）を保持するオブジェクト。
/// </summary>
[GenerateR3JsonConfigDto]
[JsonConfigDerivedType("folder_group")]
[Inject(InjectServiceLifetime.Transient)]
public class FolderGroupExecutionProgramObjectModel : DefaultExecutionProgramObjectModel {
	public FolderGroupExecutionProgramObjectModel() {
		this.MediaType = MediaType.FolderGroup;
	}
	public ReactiveProperty<ExecutionType> ExecutionType {
		get;
		set;
	} = new(Composition.Enum.ExecutionType.External);
}