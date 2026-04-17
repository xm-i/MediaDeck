using MediaDeck.Database.Tables;

namespace MediaDeck.Core.Services.DuplicateFileDetector;

/// <summary>
/// 重複ファイルグループ
/// </summary>
public class DuplicateFileGroup {
	/// <summary>
	/// ハッシュ値
	/// </summary>
	public required string Hash {
		get;
		init;
	}

	/// <summary>
	/// 重複ファイルリスト
	/// </summary>
	public required List<MediaFile> Files {
		get;
		init;
	}

	/// <summary>
	/// 代表ファイル名（表示用）
	/// </summary>
	public string RepresentativeFileName {
		get {
			return this.Files.FirstOrDefault()?.FilePath ?? this.Hash;
		}
	}
}