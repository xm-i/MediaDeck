using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

using MediaDeck.Composition.Interfaces.Services;

using Microsoft.Extensions.Logging;

using Windows.Storage.Pickers;

using WinRT.Interop;

namespace MediaDeck.Services;

/// <summary>
/// WinUI 3 の <see cref="FileOpenPicker"/> および <see cref="FolderPicker"/> を用いて
/// <see cref="IFilePickerService"/> を実装するサービス。
/// </summary>
[Inject(InjectServiceLifetime.Singleton, typeof(IFilePickerService))]
public partial class FilePickerService(ILogger<FilePickerService> logger) : IFilePickerService {
	private readonly ILogger<FilePickerService> _logger = logger;

	[LibraryImport("user32.dll")]
	private static partial nint GetActiveWindow();

	private static nint ResolveWindowHandle(object? parentWindow) {
		if (parentWindow is Microsoft.UI.Xaml.Window window) {
			return WindowNative.GetWindowHandle(window);
		}
		return GetActiveWindow();
	}

	/// <inheritdoc />
	public async Task<byte[]?> PickImageAsync() {
		var hWnd = ResolveWindowHandle(null);

		var picker = new FileOpenPicker {
			ViewMode = PickerViewMode.Thumbnail,
			SuggestedStartLocation = PickerLocationId.PicturesLibrary,
		};
		picker.FileTypeFilter.Add(".png");
		picker.FileTypeFilter.Add(".jpg");
		picker.FileTypeFilter.Add(".jpeg");
		picker.FileTypeFilter.Add(".bmp");
		picker.FileTypeFilter.Add(".gif");
		picker.FileTypeFilter.Add(".webp");

		if (hWnd != 0) {
			InitializeWithWindow.Initialize(picker, hWnd);
		}

		var file = await picker.PickSingleFileAsync();
		if (file is null) {
			return null;
		}
		try {
			return await File.ReadAllBytesAsync(file.Path);
		} catch (Exception ex) {
			this._logger.LogError(ex, "Failed to read picked image at {Path}", file.Path);
			return null;
		}
	}

	/// <inheritdoc />
	public async Task<string?> PickFolderAsync(object? parentWindow = null) {
		var hWnd = ResolveWindowHandle(parentWindow);

		var picker = new FolderPicker {
			SuggestedStartLocation = PickerLocationId.Desktop,
		};
		picker.FileTypeFilter.Add("*");

		if (hWnd != 0) {
			InitializeWithWindow.Initialize(picker, hWnd);
		}

		try {
			var folder = await picker.PickSingleFolderAsync();
			return folder?.Path;
		} catch (Exception ex) {
			this._logger.LogError(ex, "Failed to pick folder");
			return null;
		}
	}
}