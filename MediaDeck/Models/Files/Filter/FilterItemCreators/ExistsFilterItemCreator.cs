using System.IO;

using MediaDeck.Models.Files.Filter.FilterItemObjects;

namespace MediaDeck.Models.Files.Filter.FilterItemCreators;
/// <summary>
/// ファイル存在フィルタークリエイター
/// </summary>
[AddTransient]
public class ExistsFilterItemCreator : IFilterItemCreator<ExistsFilterItemObject> {
	/// <summary>
	/// フィルター条件の作成
	/// </summary>
	/// <returns>作成された条件</returns>
	public FilterItem Create(ExistsFilterItemObject filterItemObject) {
		return new FilterItem(
			x => File.Exists(x.FilePath) == filterItemObject.Exists,
			x => x.Exists == filterItemObject.Exists,
			false);
	}
}