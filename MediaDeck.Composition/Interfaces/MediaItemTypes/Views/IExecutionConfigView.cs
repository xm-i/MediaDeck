namespace MediaDeck.Composition.Interfaces.MediaItemTypes.Views;

/// <summary>
/// メディアタイプ固有の実行設定UIを表すインターフェース。
/// 各メディアタイプがカスタム設定UIを提供する場合に実装する。
/// </summary>
public interface IExecutionConfigView {
	/// <summary>
	/// データコンテキスト
	/// </summary>
	public object DataContext {
		get;
		set;
	}
}