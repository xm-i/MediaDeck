namespace MediaDeck.Core.Models.Repositories;

/// <summary>
/// アルバムのリネーム処理の結果を表します。
/// </summary>
public enum AlbumRenameResult {
	/// <summary>
	/// リネームに成功しました（パスに変更がない場合も含みます）。
	/// </summary>
	Success,

	/// <summary>
	/// 変更後のパスが変更元アルバムの配下であるため、リネームできません。
	/// </summary>
	InvalidDestinationUnderSource,

	/// <summary>
	/// 変更先に同じパスのアルバムまたは親グループが既に存在するため、リネームできません。
	/// </summary>
	DuplicatePath,

	/// <summary>
	/// リネーム対象のアルバムが見つかりませんでした。
	/// </summary>
	NotFound,
}