using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using MediaDeck.Common.Base;
using MediaDeck.Composition.Objects;
using Microsoft.Extensions.Logging;

namespace MediaDeck.Core.Services.Feedback;

/// <summary>
/// フィードバック用の環境情報・ログ取得およびHTTP送信を行うサービス
/// </summary>
[Inject(InjectServiceLifetime.Singleton, typeof(IFeedbackService))]
public class FeedbackService : ServiceBase, IFeedbackService {
	/// <summary>
	/// デフォルトのフィードバック送信先エンドポイントURL
	/// </summary>
	public const string DefaultEndpointUrl = "https://mediadeck.xm-i.net/api/feedback";

	private readonly IAppPathProvider _appPathProvider;
	private readonly ILogger<FeedbackService> _logger;
	private readonly HttpClient _httpClient;
	private readonly string _endpointUrl;

	/// <summary>
	/// <see cref="FeedbackService"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	/// <param name="appPathProvider">アプリケーションパスプロバイダー</param>
	/// <param name="logger">ロガー</param>
	public FeedbackService(IAppPathProvider appPathProvider, ILogger<FeedbackService> logger) {
		this._appPathProvider = appPathProvider;
		this._logger = logger;
		this._endpointUrl = DefaultEndpointUrl;
		this._httpClient = new HttpClient().AddTo(this.CompositeDisposable);
		this._httpClient.Timeout = TimeSpan.FromSeconds(30);
	}

	/// <summary>
	/// 単体テスト用に <see cref="HttpMessageHandler"/> および送信先URLを指定して <see cref="FeedbackService"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	/// <param name="appPathProvider">アプリケーションパスプロバイダー</param>
	/// <param name="logger">ロガー</param>
	/// <param name="httpMessageHandler">HTTPメッセージハンドラー</param>
	/// <param name="endpointUrl">送信先エンドポイントURL</param>
	internal FeedbackService(
		IAppPathProvider appPathProvider,
		ILogger<FeedbackService> logger,
		HttpMessageHandler httpMessageHandler,
		string endpointUrl = DefaultEndpointUrl) {
		this._appPathProvider = appPathProvider;
		this._logger = logger;
		this._endpointUrl = endpointUrl;
		this._httpClient = new HttpClient(httpMessageHandler, disposeHandler: false).AddTo(this.CompositeDisposable);
		this._httpClient.Timeout = TimeSpan.FromSeconds(30);
	}

	/// <inheritdoc />
	public string GetEnvironmentInfo() {
		var appVersion = Assembly.GetEntryAssembly()?.GetName().Version?.ToString()
			?? typeof(FeedbackService).Assembly.GetName().Version?.ToString()
			?? "Unknown";

		var sb = new StringBuilder();
		sb.AppendLine($"App Version: {appVersion}");
		sb.AppendLine($"OS: {RuntimeInformation.OSDescription}");
		sb.AppendLine($"OS Architecture: {RuntimeInformation.OSArchitecture}");
		sb.AppendLine($"Process Architecture: {RuntimeInformation.ProcessArchitecture}");
		sb.AppendLine($"Runtime: {RuntimeInformation.FrameworkDescription}");
		sb.AppendLine($"Culture: {CultureInfo.CurrentUICulture.Name}");
		return sb.ToString().TrimEnd();
	}

	/// <inheritdoc />
	public async Task<string> GetRecentLogsAsync(int maxLines = 100, CancellationToken cancellationToken = default) {
		if (maxLines <= 0) {
			return string.Empty;
		}

		var logDirectory = Path.Combine(this._appPathProvider.BaseDirectory, "log");
		if (!Directory.Exists(logDirectory)) {
			return string.Empty;
		}

		try {
			var latestLogFile = new DirectoryInfo(logDirectory)
				.EnumerateFiles("*.log")
				.OrderByDescending(file => file.LastWriteTimeUtc)
				.FirstOrDefault();

			if (latestLogFile == null) {
				return string.Empty;
			}

			using var fileStream = new FileStream(
				latestLogFile.FullName,
				FileMode.Open,
				FileAccess.Read,
				FileShare.ReadWrite | FileShare.Delete);
			using var reader = new StreamReader(fileStream, Encoding.UTF8);

			var lines = new Queue<string>(maxLines);
			while (await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false) is { } line) {
				if (lines.Count == maxLines) {
					lines.Dequeue();
				}
				lines.Enqueue(line);
			}

			return string.Join(Environment.NewLine, lines);
		} catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
			throw;
		} catch (Exception ex) {
			this._logger.LogWarning(ex, "直近のログファイルの読み込みに失敗しました。");
			return string.Empty;
		}
	}

	/// <inheritdoc />
	public async Task<bool> SendFeedbackAsync(FeedbackPayload payload, CancellationToken cancellationToken = default) {
		try {
			var json = JsonSerializer.Serialize(payload, FeedbackJsonSerializerContext.Default.FeedbackPayload);
			using var content = new StringContent(json, Encoding.UTF8, "application/json");
			using var response = await this._httpClient.PostAsync(this._endpointUrl, content, cancellationToken).ConfigureAwait(false);

			if (response.IsSuccessStatusCode) {
				return true;
			}

			this._logger.LogWarning("フィードバック送信に失敗しました。StatusCode: {StatusCode}", response.StatusCode);
			return false;
		} catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
			throw;
		} catch (Exception ex) {
			this._logger.LogError(ex, "フィードバック送信中にエラーが発生しました。");
			return false;
		}
	}
}