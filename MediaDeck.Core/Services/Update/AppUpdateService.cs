using System.Text.Json;
using MediaDeck.Common.Base;
using MediaDeck.Composition.Objects;
using Microsoft.Extensions.Logging;

namespace MediaDeck.Core.Services.Update;

/// <summary>
/// GitHub Releases を参照してアプリケーションの更新確認を行うサービス
/// </summary>
[Inject(InjectServiceLifetime.Singleton, typeof(IAppUpdateService))]
public class AppUpdateService : ServiceBase, IAppUpdateService {
	/// <summary>
	/// GitHub 最新リリース取得APIのデフォルトURL
	/// </summary>
	public const string DefaultLatestReleaseApiUrl = "https://api.github.com/repos/xm-i/MediaDeck/releases/latest";

	/// <summary>
	/// リリース一覧ページのフォールバックURL
	/// </summary>
	public const string FallbackReleasesPageUrl = "https://github.com/xm-i/MediaDeck/releases";

	private readonly IAppEnvironmentService _appEnvironmentService;
	private readonly ILogger<AppUpdateService> _logger;
	private readonly HttpClient _httpClient;
	private readonly string _endpointUrl;
	private readonly SemaphoreSlim _checkLock;
	private readonly ReactiveProperty<AppUpdateInfo?> _availableUpdate;
	private readonly ReactiveProperty<bool> _isChecking;

	/// <summary>
	/// <see cref="AppUpdateService"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	/// <param name="appEnvironmentService">アプリケーション環境サービス</param>
	/// <param name="logger">ロガー</param>
	public AppUpdateService(IAppEnvironmentService appEnvironmentService, ILogger<AppUpdateService> logger) {
		this._appEnvironmentService = appEnvironmentService;
		this._logger = logger;
		this._endpointUrl = DefaultLatestReleaseApiUrl;
		this._httpClient = CreateHttpClient(appEnvironmentService.CurrentVersion, null).AddTo(this.CompositeDisposable);
		this._checkLock = new SemaphoreSlim(1, 1).AddTo(this.CompositeDisposable);
		this._availableUpdate = new ReactiveProperty<AppUpdateInfo?>(null).AddTo(this.CompositeDisposable);
		this._isChecking = new ReactiveProperty<bool>(false).AddTo(this.CompositeDisposable);
		this.AvailableUpdate = this._availableUpdate.ToReadOnlyReactiveProperty().AddTo(this.CompositeDisposable);
		this.IsChecking = this._isChecking.ToReadOnlyReactiveProperty().AddTo(this.CompositeDisposable);
	}

	/// <summary>
	/// 単体テスト用に <see cref="HttpMessageHandler"/> および取得先URLを指定して <see cref="AppUpdateService"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	/// <param name="appEnvironmentService">アプリケーション環境サービス</param>
	/// <param name="logger">ロガー</param>
	/// <param name="httpMessageHandler">HTTPメッセージハンドラー</param>
	/// <param name="endpointUrl">最新リリースAPIのURL</param>
	internal AppUpdateService(
		IAppEnvironmentService appEnvironmentService,
		ILogger<AppUpdateService> logger,
		HttpMessageHandler httpMessageHandler,
		string endpointUrl = DefaultLatestReleaseApiUrl) {
		this._appEnvironmentService = appEnvironmentService;
		this._logger = logger;
		this._endpointUrl = endpointUrl;
		this._httpClient = CreateHttpClient(appEnvironmentService.CurrentVersion, httpMessageHandler).AddTo(this.CompositeDisposable);
		this._checkLock = new SemaphoreSlim(1, 1).AddTo(this.CompositeDisposable);
		this._availableUpdate = new ReactiveProperty<AppUpdateInfo?>(null).AddTo(this.CompositeDisposable);
		this._isChecking = new ReactiveProperty<bool>(false).AddTo(this.CompositeDisposable);
		this.AvailableUpdate = this._availableUpdate.ToReadOnlyReactiveProperty().AddTo(this.CompositeDisposable);
		this.IsChecking = this._isChecking.ToReadOnlyReactiveProperty().AddTo(this.CompositeDisposable);
	}

	/// <inheritdoc />
	public ReadOnlyReactiveProperty<AppUpdateInfo?> AvailableUpdate {
		get;
	}

	/// <inheritdoc />
	public ReadOnlyReactiveProperty<bool> IsChecking {
		get;
	}

	/// <inheritdoc />
	public async Task<UpdateCheckResult> CheckForUpdatesAsync(CancellationToken cancellationToken = default) {
		if (!this._appEnvironmentService.IsUnpackaged) {
			return new UpdateCheckResult(UpdateCheckStatus.NotSupported);
		}

		if (this.DisposeState != Composition.Enum.DisposeState.NotDisposed) {
			return new UpdateCheckResult(UpdateCheckStatus.Failed);
		}

		var lockAcquired = false;
		try {
			await this._checkLock.WaitAsync(cancellationToken).ConfigureAwait(false);
			lockAcquired = true;

			if (this.DisposeState != Composition.Enum.DisposeState.NotDisposed) {
				return new UpdateCheckResult(UpdateCheckStatus.Failed);
			}

			this._isChecking.Value = true;

			using var request = new HttpRequestMessage(HttpMethod.Get, this._endpointUrl);
			request.Headers.Accept.ParseAdd("application/vnd.github+json");

			using var response = await this._httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
			if (!response.IsSuccessStatusCode) {
				this._logger.LogWarning("アップデート確認に失敗しました。StatusCode: {StatusCode}", response.StatusCode);
				return new UpdateCheckResult(UpdateCheckStatus.Failed);
			}

			var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
			var release = JsonSerializer.Deserialize(json, GitHubReleaseJsonSerializerContext.Default.GitHubReleaseResponse);
			if ((release == null) || release.Draft || release.Prerelease) {
				this._logger.LogWarning("有効なリリース情報を取得できませんでした。");
				return new UpdateCheckResult(UpdateCheckStatus.Failed);
			}

			if (!TryParseVersion(release.TagName, out var latestVersion) || (latestVersion == null)) {
				this._logger.LogWarning("リリースのタグ名をバージョンとして解析できませんでした。TagName: {TagName}", release.TagName);
				return new UpdateCheckResult(UpdateCheckStatus.Failed);
			}

			var normalizedCurrent = NormalizeVersion(this._appEnvironmentService.CurrentVersion);
			var normalizedLatest = NormalizeVersion(latestVersion);

			if (normalizedLatest > normalizedCurrent) {
				var tagName = release.TagName!.Trim();
				var releaseName = string.IsNullOrWhiteSpace(release.Name) ? tagName : release.Name.Trim();
				var updateInfo = new AppUpdateInfo {
					TagName = tagName,
					VersionText = FormatVersion(normalizedLatest),
					CurrentVersionText = FormatVersion(normalizedCurrent),
					ReleaseName = releaseName,
					ReleaseNotes = release.Body?.Trim() ?? string.Empty,
					HtmlUrl = ResolveSafeHtmlUrl(release.HtmlUrl),
					PublishedAt = release.PublishedAt
				};

				if (this.DisposeState == Composition.Enum.DisposeState.NotDisposed) {
					this._availableUpdate.Value = updateInfo;
				}
				return new UpdateCheckResult(UpdateCheckStatus.UpdateAvailable, updateInfo);
			}

			if (this.DisposeState == Composition.Enum.DisposeState.NotDisposed) {
				this._availableUpdate.Value = null;
			}
			return new UpdateCheckResult(UpdateCheckStatus.UpToDate);
		} catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
			throw;
		} catch (ObjectDisposedException) {
			return new UpdateCheckResult(UpdateCheckStatus.Failed);
		} catch (Exception ex) {
			this._logger.LogWarning(ex, "アップデート確認中にエラーが発生しました。");
			return new UpdateCheckResult(UpdateCheckStatus.Failed);
		} finally {
			if (this.DisposeState == Composition.Enum.DisposeState.NotDisposed) {
				this._isChecking.Value = false;
			}
			if (lockAcquired) {
				try {
					this._checkLock.Release();
				} catch (ObjectDisposedException) {
					// 破棄済みの場合の解放は無視する
				}
			}
		}
	}

	/// <summary>
	/// タグ文字列（例: <c>v1.2.0</c>）からバージョンを解析します。
	/// プレリリース表記（ハイフンを含むタグ）は対象外とします。
	/// </summary>
	/// <param name="tagName">解析対象のタグ名</param>
	/// <param name="version">解析されたバージョン</param>
	/// <returns>解析に成功した場合は <c>true</c></returns>
	internal static bool TryParseVersion(string? tagName, out Version? version) {
		version = null;
		if (string.IsNullOrWhiteSpace(tagName)) {
			return false;
		}

		var trimmed = tagName.Trim();
		if (trimmed.StartsWith('v') || trimmed.StartsWith('V')) {
			trimmed = trimmed[1..];
		}

		var plusIndex = trimmed.IndexOf('+');
		if (plusIndex >= 0) {
			trimmed = trimmed[..plusIndex];
		}

		if (trimmed.Contains('-')) {
			return false;
		}

		if (!Version.TryParse(trimmed, out var parsed)) {
			return false;
		}

		version = NormalizeVersion(parsed);
		return true;
	}

	/// <summary>
	/// <see cref="Version"/> の未指定フィールド（-1）を 0 に正規化します。
	/// </summary>
	/// <param name="version">正規化前のバージョン</param>
	/// <returns>4要素に正規化されたバージョン</returns>
	internal static Version NormalizeVersion(Version version) {
		var build = version.Build >= 0 ? version.Build : 0;
		var revision = version.Revision >= 0 ? version.Revision : 0;
		return new Version(version.Major, version.Minor, build, revision);
	}

	/// <summary>
	/// 表示用のバージョン文字列（例: <c>1.1.0</c>）に整形します。
	/// </summary>
	/// <param name="version">対象のバージョン</param>
	/// <returns>整形されたバージョン文字列</returns>
	public static string FormatVersion(Version version) {
		var normalized = NormalizeVersion(version);
		return normalized.Revision > 0
			? $"{normalized.Major}.{normalized.Minor}.{normalized.Build}.{normalized.Revision}"
			: $"{normalized.Major}.{normalized.Minor}.{normalized.Build}";
	}

	/// <summary>
	/// リリースURLが安全なHTTPSスキームであるか検証し、無効な場合はフォールバックURLを返します。
	/// </summary>
	/// <param name="htmlUrl">検証対象のURL文字列</param>
	/// <returns>安全なリリースページURL</returns>
	internal static string ResolveSafeHtmlUrl(string? htmlUrl) {
		if (Uri.TryCreate(htmlUrl, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttps)) {
			return uri.ToString();
		}
		return FallbackReleasesPageUrl;
	}

	/// <summary>
	/// アップデート確認用の <see cref="HttpClient"/> を生成します。
	/// </summary>
	/// <param name="currentVersion">現在のアプリケーションバージョン</param>
	/// <param name="handler">差し替え用のHTTPメッセージハンドラー（省略時はデフォルト）</param>
	/// <returns>構成済みの <see cref="HttpClient"/></returns>
	private static HttpClient CreateHttpClient(Version currentVersion, HttpMessageHandler? handler) {
		var client = handler == null
			? new HttpClient()
			: new HttpClient(handler, disposeHandler: false);
		client.Timeout = TimeSpan.FromSeconds(15);

		var versionText = FormatVersion(currentVersion);
		client.DefaultRequestHeaders.UserAgent.ParseAdd($"MediaDeck/{versionText} (+https://github.com/xm-i/MediaDeck)");
		return client;
	}
}