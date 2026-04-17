namespace MediaDeck.Core.Services.FileChangeMonitor;

/// <summary>
/// ファイルの変更種類を定義します。
/// </summary>
public enum FileChangeType {
	/// <summary>削除</summary>
	Deleted,

	/// <summary>同一ドライブ内のリネーム・移動</summary>
	Renamed,

	/// <summary>別ドライブ間での移動（ハッシュ一致で確定）</summary>
	Moved,

	/// <summary>新規追加</summary>
	Added
}