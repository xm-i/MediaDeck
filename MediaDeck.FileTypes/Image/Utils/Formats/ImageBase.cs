using System.IO;

using MetadataExtractor;

namespace MediaDeck.FileTypes.Image.Utils.Formats;

/// <summary>
/// 各種イメージフォーマットの基底クラス
/// </summary>
/// <remarks>
/// コンストラクタで受け取ったStreamをDisposeする。
/// </remarks>
internal abstract class ImageBase : IImage {
	private bool _disposedValue;
	private readonly Stream _stream;

	/// <inheritdoc/>
	public int Width {
		get;
		protected internal init;
	}

	/// <inheritdoc/>
	public int Height {
		get;
		protected internal init;
	}

	/// <inheritdoc/>
	public Rational[]? Latitude {
		get;
		protected internal init;
	}

	/// <inheritdoc/>
	public Rational[]? Longitude {
		get;
		protected internal init;
	}

	/// <inheritdoc/>
	public Rational? Altitude {
		get;
		protected internal init;
	}

	/// <inheritdoc/>
	public string? LatitudeRef {
		get;
		protected internal init;
	}

	/// <inheritdoc/>
	public string? LongitudeRef {
		get;
		protected internal init;
	}

	/// <inheritdoc/>
	public byte? AltitudeRef {
		get;
		protected internal init;
	}

	/// <inheritdoc/>
	public int? Orientation {
		get;
		protected internal init;
	}

	/// <summary>
	/// コンストラクタ
	/// </summary>
	/// <param name="stream">破棄するためのStreamオブジェクト</param>
	protected internal ImageBase(Stream stream) {
		this._stream = stream;
	}

	/// <summary>
	/// 文字取得
	/// </summary>
	/// <param name="directory">ディレクトリ</param>
	/// <param name="tag">タグ</param>
	/// <returns>取得した値</returns>
	protected string? GetString(MetadataExtractor.Directory? directory, int tag) {
		return directory?.GetString(tag);
	}

	/// <summary>
	/// 整数取得
	/// </summary>
	/// <param name="directory">ディレクトリ</param>
	/// <param name="tag">タグ</param>
	/// <returns>取得した値</returns>
	protected short? GetShort(MetadataExtractor.Directory? directory, int tag) {
		if (directory == null) {
			return default;
		}
		if (directory.TryGetInt16(tag, out var value)) {
			return value;
		}
		return default;
	}

	/// <summary>
	/// 整数取得
	/// </summary>
	/// <param name="directory">ディレクトリ</param>
	/// <param name="tag">タグ</param>
	/// <returns>取得した値</returns>
	protected int? GetInt(MetadataExtractor.Directory? directory, int tag) {
		if (directory == null) {
			return default;
		}
		if (directory.TryGetInt32(tag, out var value)) {
			return value;
		}
		return default;
	}

	/// <summary>
	/// 小数取得
	/// </summary>
	/// <param name="directory">ディレクトリ</param>
	/// <param name="tag">タグ</param>
	/// <returns>取得した値</returns>
	protected double? GetDouble(MetadataExtractor.Directory? directory, int tag) {
		if (directory == null) {
			return default;
		}
		if (directory.TryGetDouble(tag, out var value)) {
			return value;
		}
		return default;
	}

	/// <summary>
	/// 日付取得
	/// </summary>
	/// <param name="directory">ディレクトリ</param>
	/// <param name="tag">タグ</param>
	/// <returns>取得した値</returns>
	protected DateTime? GetDateTime(MetadataExtractor.Directory? directory, int tag) {
		if (directory == null) {
			return default;
		}
		if (directory.TryGetDateTime(tag, out var value)) {
			return value;
		}
		return default;
	}

	/// <summary>
	/// バイナリ取得
	/// </summary>
	/// <param name="directory">ディレクトリ</param>
	/// <param name="tag">タグ</param>
	/// <returns>取得した値</returns>
	protected byte[]? GetBinary(MetadataExtractor.Directory? directory, int tag) {
		return directory?.GetByteArray(tag);
	}

	/// <summary>
	/// 分数取得
	/// </summary>
	/// <param name="directory">ディレクトリ</param>
	/// <param name="tag">タグ</param>
	/// <returns>取得した値</returns>
	protected (long?, long?) GetRational(MetadataExtractor.Directory? directory, int tag) {
		if (directory == null) {
			return default;
		}
		if (directory.TryGetRational(tag, out var value)) {
			return (value.Denominator, value.Numerator);
		}
		return default;
	}

	/// <summary>
	/// 座標取得
	/// </summary>
	/// <param name="directory">ディレクトリ</param>
	/// <param name="tag">タグ</param>
	/// <returns>取得した値</returns>
	protected (double?, double?, double?) Get3Rational(MetadataExtractor.Directory? directory, int tag) {
		if (directory == null) {
			return default;
		}
		var value = directory.GetRationalArray(tag);
		if (value?.Length == 3) {
			return (value[0].ToDouble(), value[1].ToDouble(), value[2].ToDouble());
		}

		return default;
	}

	/// <summary>
	/// Dispose
	/// </summary>
	/// <param name="disposing">マネージドリソースを破棄するか</param>
	protected virtual void Dispose(bool disposing) {
		if (!this._disposedValue) {
			if (disposing) {
				this._stream.Dispose();
			}
			this._disposedValue = true;
		}
	}

	/// <inheritdoc/>
	public void Dispose() {
		this.Dispose(true);
		GC.SuppressFinalize(this);
	}
}