using MediaDeck.Composition.Interfaces.Files;

namespace MediaDeck.Composition.Interfaces.Notifications;

/// <summary>
/// フォルダパスに基づく検索条件のインターフェース。
/// </summary>
public interface IFolderSearchCondition : ISearchCondition {
	/// <summary>
	/// 対象のフォルダパス
	/// </summary>
	public string FolderPath {
		get; set;
	}

	/// <summary>
	/// サブディレクトリを含めるかどうか
	/// </summary>
	public bool IncludeSubDirectories {
		get; set;
	}
}