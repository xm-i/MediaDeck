using MetadataExtractor;

namespace MediaDeck.MediaItemTypes.Image.Utils.Formats;

/// <summary>
/// 画像ファイルメタデータ取得インターフェイス
/// </summary>
public interface IImage : IDisposable {
	/// <summary>
	/// 幅
	/// </summary>
	public int Width {
		get;
	}

	/// <summary>
	/// 高さ
	/// </summary>
	public int Height {
		get;
	}

	/// <summary>
	/// 緯度
	/// </summary>
	public Rational[]? Latitude {
		get;
	}

	/// <summary>
	/// 経度
	/// </summary>
	public Rational[]? Longitude {
		get;
	}

	/// <summary>
	/// 高度
	/// </summary>
	public Rational? Altitude {
		get;
	}

	/// <summary>
	/// 緯度方向(N/S)
	/// </summary>
	public string? LatitudeRef {
		get;
	}

	/// <summary>
	/// 経度方向(E/W)
	/// </summary>
	public string? LongitudeRef {
		get;
	}

	/// <summary>
	/// 高度方向(0/1)
	/// </summary>
	public byte? AltitudeRef {
		get;
	}

	/// <summary>
	/// 画像の方向
	/// </summary>
	public int? Orientation {
		get;
	}
}