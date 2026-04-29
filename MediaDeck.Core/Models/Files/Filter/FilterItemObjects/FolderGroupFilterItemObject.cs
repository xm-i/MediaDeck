using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.Files;
using R3.JsonConfig.Attributes;

namespace MediaDeck.Core.Models.Files.Filter.FilterItemObjects;

/// <summary>
/// フォルダグループフィルターアイテムオブジェクト
/// </summary>
[GenerateR3JsonConfigDto]
[JsonConfigDerivedType("folderGroupFilter")]
[Inject(InjectServiceLifetime.Transient)]
public class FolderGroupFilterItemObject : IFilterItemObject {
	/// <summary>
	/// 表示名
	/// </summary>
	public string DisplayName {
		get {
			return $"{(this.SearchType == SearchTypeInclude.Include ? "Include" : "Exclude")} files in folder group";
		}
	}

	/// <summary>
	/// 検索タイプ
	/// </summary>
	public SearchTypeInclude SearchType {
		get;
		set;
	}

	public FolderGroupFilterItemObject() {
	}
}