using System.IO;

namespace MediaDeck.Models.Services;

/// <summary>
/// 未処理のファイル変更を保持するアイテムクラスです。
/// </summary>
public class FileChangeItem {
	/// <summary>
	/// 既存のメディアファイルID（新規追加の場合はnull）
	/// </summary>
	public long? MediaFileId {
		get; set;
	}

	/// <summary>
	/// イベント発生時刻
	/// </summary>
	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

	/// <summary>
	/// DB検索やハッシュ計算が未完了かどうか
	/// </summary>
	public bool IsPending { get; set; }

	/// <summary>
	/// 変更前のパス
	/// </summary>
	public string OldPath { get; set; } = string.Empty;

	/// <summary>
	/// 変更後のパス
	/// </summary>
	public string NewPath { get; set; } = string.Empty;

	/// <summary>
	/// 変更の種類
	/// </summary>
	public FileChangeType ChangeType {
		get; set;
	}

	/// <summary>
	/// 現在ハッシュ計算中かどうか
	/// </summary>
	public bool IsHashing {
		get; set;
	}

	/// <summary>
	/// ファイルサイズ（マッチングに使用）
	/// </summary>
	public long FileSize {
		get; set;
	}

	/// <summary>
	/// 変更前のハッシュ値
	/// </summary>
	public string OldHash { get; set; } = string.Empty;

	/// <summary>
	/// 変更後のハッシュ値
	/// </summary>
	public string NewHash { get; set; } = string.Empty;

	/// <summary>
	/// UI表示用の変更種別テキスト
	/// </summary>
	public string ChangeTypeText {
		get {
			return this.ChangeType switch {
				FileChangeType.Deleted => "削除",
				FileChangeType.Renamed => "変更",
				FileChangeType.Moved => "移動",
				FileChangeType.Added => "追加",
				_ => "不明"
			};
		}
	}
}
