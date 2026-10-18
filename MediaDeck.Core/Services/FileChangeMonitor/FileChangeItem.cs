using MediaDeck.Composition.Interfaces;

namespace MediaDeck.Core.Services.FileChangeMonitor;

/// <summary>
/// 未処理のファイル変更を保持するアイテムクラスです。
/// </summary>
public class FileChangeItem {
	private readonly IStringProvider? _stringProvider;

	/// <summary>
	/// <see cref="FileChangeItem"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	/// <param name="stringProvider">文字列プロバイダー（省略可能）</param>
	public FileChangeItem(IStringProvider? stringProvider = null) {
		this._stringProvider = stringProvider;
	}
	/// <summary>
	/// 既存のメディアアイテムID（新規追加の場合はnull）
	/// </summary>
	public long? MediaItemId {
		get;
		set;
	}

	/// <summary>
	/// イベント発生時刻
	/// </summary>
	public DateTime CreatedAt {
		get;
		set;
	} = DateTime.UtcNow;

	/// <summary>
	/// DB検索やハッシュ計算が未完了かどうか
	/// </summary>
	public bool IsPending {
		get;
		set;
	}

	/// <summary>
	/// 変更前のパス
	/// </summary>
	public string OldPath {
		get;
		set;
	} = string.Empty;

	/// <summary>
	/// 変更後のパス
	/// </summary>
	public string NewPath {
		get;
		set;
	} = string.Empty;

	/// <summary>
	/// 変更の種類
	/// </summary>
	public FileChangeType ChangeType {
		get;
		set;
	}

	/// <summary>
	/// 現在ハッシュ計算中かどうか
	/// </summary>
	public bool IsHashing {
		get;
		set;
	}

	/// <summary>
	/// ファイルサイズ（マッチングに使用）
	/// </summary>
	public long FileSize {
		get;
		set;
	}

	/// <summary>
	/// 変更前のハッシュ値
	/// </summary>
	public string OldHash {
		get;
		set;
	} = string.Empty;

	/// <summary>
	/// 変更後のハッシュ値
	/// </summary>
	public string NewHash {
		get;
		set;
	} = string.Empty;

	/// <summary>
	/// UI表示用の変更種別テキスト
	/// </summary>
	public string ChangeTypeText {
		get {
			if (this._stringProvider != null) {
				return this.GetChangeTypeText(this._stringProvider);
			}
			return this.ChangeType switch {
				FileChangeType.Deleted => "削除",
				FileChangeType.Renamed => "変更",
				FileChangeType.Moved => "移動",
				FileChangeType.Added => "追加",
				_ => "不明"
			};
		}
	}

	/// <summary>
	/// 指定した <see cref="IStringProvider"/> を使用してUI表示用の変更種別テキストを取得します。
	/// </summary>
	/// <param name="stringProvider">文字列プロバイダー</param>
	/// <returns>変更種別の表示文字列</returns>
	public string GetChangeTypeText(IStringProvider stringProvider) {
		var key = this.ChangeType switch {
			FileChangeType.Deleted => "FileChangeType_Deleted",
			FileChangeType.Renamed => "FileChangeType_Renamed",
			FileChangeType.Moved => "FileChangeType_Moved",
			FileChangeType.Added => "FileChangeType_Added",
			_ => "FileChangeType_Unknown"
		};
		return stringProvider.GetString(key);
	}
}