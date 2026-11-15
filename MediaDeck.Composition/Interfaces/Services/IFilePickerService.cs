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

	/// <summary>
	/// フォルダを選択するダイアログを表示し、選択されたフォルダのパスを返す。
	/// </summary>
	/// <param name="parentWindow">親ウィンドウ（省略可能）。</param>
	/// <returns>選択されたフォルダの絶対パス。ユーザーがキャンセルした場合は <c>null</c>。</returns>
	public Task<string?> PickFolderAsync(object? parentWindow = null);
}