namespace MediaDeck.Composition.Interfaces.Services;

/// <summary>
/// ファイル選択ダイアログを提供するサービスインターフェース。
/// WinUI 等の UI 実装詳細を ViewModel から隠蔽する。
/// </summary>
public interface IFilePickerService {
	/// <summary>
	/// 画像ファイルを選択するダイアログを表示し、選択されたファイルのバイト列を返す。
	/// </summary>
	/// <returns>選択された画像のバイト列。ユーザーがキャンセルした場合や読み込みに失敗した場合は <c>null</c>。</returns>
	public Task<byte[]?> PickImageAsync();
}