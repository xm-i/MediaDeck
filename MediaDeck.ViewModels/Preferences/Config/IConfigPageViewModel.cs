namespace MediaDeck.ViewModels.Preferences.Config;

/// <summary>
/// 設定ページ ViewModel のインターフェース
/// </summary>
public interface IConfigPageViewModel {
	/// <summary>
	/// ページ名
	/// </summary>
	public string PageName {
		get;
	}

	/// <summary>
	/// ページのアイコン（Segoe Fluent Icons のグリフ文字）
	/// </summary>
	public string PageIconGlyph {
		get;
	}

	/// <summary>
	/// ページの説明
	/// </summary>
	public string PageDescription {
		get;
	}
}