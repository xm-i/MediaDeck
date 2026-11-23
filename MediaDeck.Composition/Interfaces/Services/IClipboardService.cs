namespace MediaDeck.Composition.Interfaces.Services;

/// <summary>
/// クリップボード操作を提供するサービスインターフェース。
/// WinUI 等の UI / OS 実装詳細を ViewModel から隠蔽します。
/// </summary>
public interface IClipboardService : IServiceBase {
	/// <summary>
	/// 指定されたテキストをクリップボードに設定します。
	/// </summary>
	/// <param name="text">クリップボードにコピーするテキスト。</param>
	public void SetText(string text);
}