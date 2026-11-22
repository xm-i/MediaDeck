namespace MediaDeck.ViewModels.Filters.FilterItemCreators;

/// <summary>
/// フィルター条件作成ビューの種類を表す列挙型。
/// </summary>
public enum FilterCreatorKind {
	/// <summary>
	/// タグフィルター
	/// </summary>
	Tag,

	/// <summary>
	/// ファイルパスフィルター
	/// </summary>
	FilePath,

	/// <summary>
	/// メディアタイプフィルター
	/// </summary>
	MediaType,

	/// <summary>
	/// 評価フィルター
	/// </summary>
	Rate,

	/// <summary>
	/// 解像度フィルター
	/// </summary>
	Resolution,

	/// <summary>
	/// 座標フィルター
	/// </summary>
	Location,

	/// <summary>
	/// フォルダグループフィルター
	/// </summary>
	FolderGroup,

	/// <summary>
	/// ファイル存在フィルター
	/// </summary>
	Exists
}