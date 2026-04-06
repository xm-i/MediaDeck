using R3.JsonConfig.Attributes;

namespace MediaDeck.Composition.Interfaces.Files;

[GenerateR3JsonConfigDto]
public interface IFilterItemObject {
	/// <summary>
	/// 表示名
	/// </summary>
	public string DisplayName {
		get;
	}
}