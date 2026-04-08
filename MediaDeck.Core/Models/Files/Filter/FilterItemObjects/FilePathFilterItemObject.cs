using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.Files;
using R3.JsonConfig.Attributes;

namespace MediaDeck.Core.Models.Files.Filter.FilterItemObjects;

/// <summary>
/// ファイルパスフィルターアイテムオブジェクト
/// </summary>
[GenerateR3JsonConfigDto]
[JsonConfigDerivedType("filePath")]
[Inject(InjectServiceLifetime.Transient)]
public class FilePathFilterItemObject : IFilterItemObject {
	/// <summary>
	/// 表示名
	/// </summary>
	public string DisplayName {
		get {
			return $"{this.Text} in file path {(this.SearchType == SearchTypeInclude.Include ? "includes" : "does not include")}";
		}
	}

	/// <summary>
	/// パスに含まれる文字列
	/// </summary>
	public string Text {
		get {
			return field ?? throw new InvalidOperationException($"{nameof(this.Text)} is not initialized.");
		}
		set {
			field = value;
		}
	}

	/// <summary>
	/// 検索タイプ
	/// </summary>
	public SearchTypeInclude SearchType {
		get;
		set;
	}

	public FilePathFilterItemObject() {
	}
}