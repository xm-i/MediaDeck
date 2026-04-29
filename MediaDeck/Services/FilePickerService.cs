using System.IO;
using System.Threading.Tasks;

using MediaDeck.Composition.Interfaces.Services;
using MediaDeck.Views;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Windows.Storage.Pickers;

using WinRT.Interop;

namespace MediaDeck.Services;

/// <summary>
/// WinUI 3 の <see cref="FileOpenPicker"/> を用いて
/// <see cref="IFilePickerService"/> を実装するサービス。
/// </summary>
[Inject(InjectServiceLifetime.Singleton, typeof(IFilePickerService))]
public class FilePickerService(IServiceProvider serviceProvider, ILogger<FilePickerService> logger) : IFilePickerService {
	private readonly IServiceProvider _serviceProvider = serviceProvider;
	private readonly ILogger<FilePickerService> _logger = logger;

	/// <inheritdoc />
	public async Task<byte[]?> PickImageAsync() {
		// WinUI 3 では FileOpenPicker を XAML から切り離して使う際、
		// 親ウィンドウの HWND と関連付ける必要がある。
		var mainWindow = this._serviceProvider.GetRequiredService<MainWindow>();
		var hWnd = WindowNative.GetWindowHandle(mainWindow);

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

		InitializeWithWindow.Initialize(picker, hWnd);

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
}