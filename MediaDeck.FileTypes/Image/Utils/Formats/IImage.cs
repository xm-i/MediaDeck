using MetadataExtractor;

namespace MediaDeck.FileTypes.Image.Utils.Formats;

/// <summary>
/// 画像ファイルメタデータ取得インターフェイス
/// </summary>
internal interface IImage : IDisposable {
	/// <summary>
	/// 幅
	/// </summary>
	internal int Width {
		get;
	}

	/// <summary>
	/// 高さ
	/// </summary>
	internal int Height {
		get;
	}

	/// <summary>
	/// 緯度
	/// </summary>
	internal Rational[]? Latitude {
		get;
	}

	/// <summary>
	/// 経度
	/// </summary>
	internal Rational[]? Longitude {
		get;
	}

	/// <summary>
	/// 高度
	/// </summary>
	internal Rational? Altitude {
		get;
	}

	/// <summary>
	/// 緯度方向(N/S)
	/// </summary>
	internal string? LatitudeRef {
		get;
	}

	/// <summary>
	/// 経度方向(E/W)
	/// </summary>
	internal string? LongitudeRef {
		get;
	}

	/// <summary>
	/// 高度方向(0/1)
	/// </summary>
	internal byte? AltitudeRef {
		get;
	}

	/// <summary>
	/// 画像の方向
	/// </summary>
	internal int? Orientation {
		get;
	}
}