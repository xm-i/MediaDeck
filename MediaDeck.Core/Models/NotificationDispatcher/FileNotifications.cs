using MediaDeck.Composition.Tables;

namespace MediaDeck.Core.Models.NotificationDispatcher;

/// <summary>
/// ファイルの登録および変更に関する通知を提供します。
/// </summary>
public static class FileNotifications {
	/// <summary>
	/// ファイルが新規登録された際の通知ストリーム
	/// </summary>
	public static Subject<MediaItem> FileRegistered {
		get;
	} = new();

	/// <summary>
	/// ファイル変更同期（移動・リネーム・削除など）がデータベースへ反映された際の通知ストリーム
	/// </summary>
	public static Subject<Unit> FilesChanged {
		get;
	} = new();
}