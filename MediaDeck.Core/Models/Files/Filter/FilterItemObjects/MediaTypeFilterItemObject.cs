using MediaDeck.Composition.Interfaces.Files;
using R3.JsonConfig.Attributes;

namespace MediaDeck.Core.Models.Files.Filter.FilterItemObjects;

/// <summary>
/// ファイルタイプフィルターアイテムオブジェクト
/// </summary>
[GenerateR3JsonConfigDto]
[JsonConfigDerivedType("mediaType")]
[Inject(InjectServiceLifetime.Transient)]

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

	public MediaTypeFilterItemObject() {
	}
}