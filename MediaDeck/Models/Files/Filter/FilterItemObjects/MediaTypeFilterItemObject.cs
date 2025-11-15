namespace MediaDeck.Models.Files.Filter.FilterItemObjects;
/// <summary>
/// ファイルタイプフィルターアイテムオブジェクト
/// </summary>
public class MediaTypeFilterItemObject : IFilterItemObject {
	/// <summary>
	/// 表示名
	/// </summary>
	public string DisplayName {
		get {
			if (this.IsVideo) {
				return "Video file";
			} else {
				return "Image file";
			}
		}
	}

	/// <summary>
	/// 動画ファイルか否か
	/// </summary>
	public bool IsVideo {
		get;
		set;
	}

	[Obsolete("for serialize")]
	public MediaTypeFilterItemObject() {
	}

	/// <summary>
	/// コンストラクタ
	/// </summary>
	/// <param name="isVideo">動画ファイルか否か</param>
	public MediaTypeFilterItemObject(bool isVideo) {
		this.IsVideo = isVideo;
	}
}
