using MediaDeck.Composition.Interfaces.Files;

namespace MediaDeck.Models.Files.Filter.FilterItemCreators; 
/// <summary>
/// フィルタークリエイターインターフェイス
/// </summary>
public interface IFilterItemCreator<in T> where T : IFilterItemObject {

	/// <summary>
	/// フィルター条件の作成
	/// </summary>
	/// <returns>作成された条件</returns>
	FilterItem Create(T filterItemObject);
}