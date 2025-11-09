using MediaDeck.Models.Files.Filter.FilterItemObjects;

namespace MediaDeck.Models.Files.Filter.FilterItemCreators;
/// <summary>
/// ファイルタイプフィルタークリエイター
/// </summary>
/// <remarks>
/// コンストラクタ
/// </remarks>
[AddTransient]
public class MediaTypeFilterItemCreator() : IFilterItemCreator<MediaTypeFilterItemObject> {

	/// <summary>
	/// フィルター条件の作成
	/// </summary>
	/// <returns>作成された条件</returns>
	public FilterItem Create(MediaTypeFilterItemObject filterItemObject) {
		return new FilterItem(
			x => x.FilePath.IsVideoFile() == filterItemObject.IsVideo,
			x => x.FilePath.IsVideoFile() == filterItemObject.IsVideo,
			false);
	}
}
