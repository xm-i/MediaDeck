using MediaDeck.Composition.Interfaces.Files;
using R3.JsonConfig.Attributes;

namespace MediaDeck.Core.Models.Files.Filter.FilterItemObjects;

/// <summary>
/// ファイル存在フィルターアイテムオブジェクト
/// </summary>
[GenerateR3JsonConfigDto]
[JsonConfigDerivedType("exists")]
[Inject(InjectServiceLifetime.Transient)]
public class ExistsFilterItemObject : IFilterItemObject {
	/// <summary>
	/// 表示名
	/// </summary>
	public string DisplayName {
		get {
			return $"File {(this.Exists ? "exists" : "does not exist")}";
		}
	}

	/// <summary>
	/// ファイルが存在するか否か
	/// </summary>
	public bool Exists {
		get;
		set;
	}

	public ExistsFilterItemObject() {
	}
}