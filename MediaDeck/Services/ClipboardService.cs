using MediaDeck.Common.Base;
using MediaDeck.Composition.Interfaces.Services;
using Microsoft.Extensions.Logging;
using Windows.ApplicationModel.DataTransfer;

namespace MediaDeck.Services;

/// <summary>
/// Windows の <see cref="Clipboard"/> API を用いて <see cref="IClipboardService"/> を実装するサービス。
/// </summary>
[Inject(InjectServiceLifetime.Singleton, typeof(IClipboardService))]
public class ClipboardService(ILogger<ClipboardService> logger) : ServiceBase, IClipboardService {
	private readonly ILogger<ClipboardService> _logger = logger;

	/// <inheritdoc />
	public void SetText(string text) {
		ArgumentNullException.ThrowIfNull(text);
		try {
			var dataPackage = new DataPackage();
			dataPackage.SetText(text);
			Clipboard.SetContent(dataPackage);
			Clipboard.Flush();
		} catch (Exception ex) {
			this._logger.LogError(ex, "クリップボードへのテキスト設定に失敗しました。");
		}
	}
}