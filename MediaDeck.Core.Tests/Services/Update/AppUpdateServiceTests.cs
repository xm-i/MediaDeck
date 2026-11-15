using System.Net;
using System.Text;
using MediaDeck.Composition.Interfaces.Services;
using MediaDeck.Composition.Objects;
using MediaDeck.Core.Services.Update;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;

namespace MediaDeck.Core.Tests.Services.Update;

/// <summary>
/// <see cref="AppUpdateService"/> の単体テストクラス
/// </summary>
public class AppUpdateServiceTests {
	private const string TestApiUrl = "https://example.test/repos/xm-i/MediaDeck/releases/latest";

	/// <summary>
	/// Unpackaged環境で新しいリリースが見つかった場合、<see cref="UpdateCheckStatus.UpdateAvailable"/> と更新情報を返すことを確認します。
	/// </summary>
	[Fact]
	public async Task CheckForUpdatesAsync_WhenNewerReleaseExistsOnUnpackaged_ReturnsUpdateAvailable() {
		// Arrange
		string? capturedUserAgent = null;
		string? capturedAccept = null;
		Uri? capturedUri = null;
		var publishedAt = new DateTimeOffset(2026, 11, 15, 10, 0, 0, TimeSpan.Zero);

		using var handler = new StubHttpMessageHandler((request, _) => {
			capturedUri = request.RequestUri;
			capturedUserAgent = request.Headers.UserAgent.ToString();
			capturedAccept = request.Headers.Accept.ToString();

			var json = """
				{
				  "tag_name": "v1.2.0",
				  "name": "Release v1.2.0",
				  "html_url": "https://github.com/xm-i/MediaDeck/releases/tag/v1.2.0",
				  "body": "### 新機能\n- アップデート通知を追加しました",
				  "published_at": "2026-11-15T10:00:00Z",
				  "draft": false,
				  "prerelease": false
				}
				""";
			return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) {
				Content = new StringContent(json, Encoding.UTF8, "application/json")
			});
		});

		var env = new StubAppEnvironmentService(true, new Version(1, 1, 0, 0));
		using var service = new AppUpdateService(env, NullLogger<AppUpdateService>.Instance, handler, TestApiUrl);

		// Act
		var result = await service.CheckForUpdatesAsync();

		// Assert
		capturedUri.ShouldBe(new Uri(TestApiUrl));
		capturedUserAgent.ShouldNotBeNullOrWhiteSpace();
		capturedUserAgent.ShouldContain("MediaDeck/1.1.0");
		capturedAccept.ShouldNotBeNull();
		capturedAccept.ShouldContain("application/vnd.github+json");

		result.Status.ShouldBe(UpdateCheckStatus.UpdateAvailable);
		result.UpdateInfo.ShouldNotBeNull();
		result.UpdateInfo.TagName.ShouldBe("v1.2.0");
		result.UpdateInfo.VersionText.ShouldBe("1.2.0");
		result.UpdateInfo.CurrentVersionText.ShouldBe("1.1.0");
		result.UpdateInfo.ReleaseName.ShouldBe("Release v1.2.0");
		result.UpdateInfo.ReleaseNotes.ShouldBe("### 新機能\n- アップデート通知を追加しました");
		result.UpdateInfo.HtmlUrl.ShouldBe("https://github.com/xm-i/MediaDeck/releases/tag/v1.2.0");
		result.UpdateInfo.PublishedAt.ShouldBe(publishedAt);

		service.AvailableUpdate.CurrentValue.ShouldBe(result.UpdateInfo);
		service.IsChecking.CurrentValue.ShouldBeFalse();
	}

	/// <summary>
	/// 4要素バージョン（1.1.0.0）と3要素タグ（v1.1.0）の比較で同一バージョンとして扱われることを確認します。
	/// </summary>
	[Fact]
	public async Task CheckForUpdatesAsync_WhenSameVersionWithDifferentComponentCount_ReturnsUpToDate() {
		// Arrange
		using var handler = CreateJsonResponseHandler("""
			{
			  "tag_name": "v1.1.0",
			  "name": "Release v1.1.0",
			  "html_url": "https://github.com/xm-i/MediaDeck/releases/tag/v1.1.0",
			  "body": "",
			  "draft": false,
			  "prerelease": false
			}
			""");

		var env = new StubAppEnvironmentService(true, new Version(1, 1, 0, 0));
		using var service = new AppUpdateService(env, NullLogger<AppUpdateService>.Instance, handler, TestApiUrl);

		// Act
		var result = await service.CheckForUpdatesAsync();

		// Assert
		result.Status.ShouldBe(UpdateCheckStatus.UpToDate);
		result.UpdateInfo.ShouldBeNull();
		service.AvailableUpdate.CurrentValue.ShouldBeNull();
	}

	/// <summary>
	/// 現在バージョンより古いリリースしか存在しない場合、<see cref="UpdateCheckStatus.UpToDate"/> を返すことを確認します。
	/// </summary>
	[Fact]
	public async Task CheckForUpdatesAsync_WhenOlderReleaseReturned_ReturnsUpToDate() {
		// Arrange
		using var handler = CreateJsonResponseHandler("""
			{
			  "tag_name": "v1.0.3",
			  "name": "Release v1.0.3",
			  "html_url": "https://github.com/xm-i/MediaDeck/releases/tag/v1.0.3",
			  "body": "old",
			  "draft": false,
			  "prerelease": false
			}
			""");

		var env = new StubAppEnvironmentService(true, new Version(1, 1, 0, 0));
		using var service = new AppUpdateService(env, NullLogger<AppUpdateService>.Instance, handler, TestApiUrl);

		// Act
		var result = await service.CheckForUpdatesAsync();

		// Assert
		result.Status.ShouldBe(UpdateCheckStatus.UpToDate);
		service.AvailableUpdate.CurrentValue.ShouldBeNull();
	}

	/// <summary>
	/// Packaged環境（Microsoft Store配布版）では通信せず <see cref="UpdateCheckStatus.NotSupported"/> を返すことを確認します。
	/// </summary>
	[Fact]
	public async Task CheckForUpdatesAsync_WhenPackaged_DoesNotSendRequestAndReturnsNotSupported() {
		// Arrange
		var called = false;
		using var handler = new StubHttpMessageHandler((_, _) => {
			called = true;
			return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
		});

		var env = new StubAppEnvironmentService(false, new Version(1, 1, 0, 0));
		using var service = new AppUpdateService(env, NullLogger<AppUpdateService>.Instance, handler, TestApiUrl);

		// Act
		var result = await service.CheckForUpdatesAsync();

		// Assert
		called.ShouldBeFalse();
		result.Status.ShouldBe(UpdateCheckStatus.NotSupported);
		result.UpdateInfo.ShouldBeNull();
		service.AvailableUpdate.CurrentValue.ShouldBeNull();
	}

	/// <summary>
	/// draft や prerelease のリリースは更新として通知しないことを確認します。
	/// </summary>
	[Theory]
	[InlineData(true, false)]
	[InlineData(false, true)]
	public async Task CheckForUpdatesAsync_WhenDraftOrPrerelease_ReturnsFailed(bool draft, bool prerelease) {
		// Arrange
		var json = $$"""
			{
			  "tag_name": "v9.9.9",
			  "name": "Preview",
			  "html_url": "https://github.com/xm-i/MediaDeck/releases/tag/v9.9.9",
			  "body": "preview",
			  "draft": {{draft.ToString().ToLowerInvariant()}},
			  "prerelease": {{prerelease.ToString().ToLowerInvariant()}}
			}
			""";
		using var handler = CreateJsonResponseHandler(json);

		var env = new StubAppEnvironmentService(true, new Version(1, 1, 0, 0));
		using var service = new AppUpdateService(env, NullLogger<AppUpdateService>.Instance, handler, TestApiUrl);

		// Act
		var result = await service.CheckForUpdatesAsync();

		// Assert
		result.Status.ShouldBe(UpdateCheckStatus.Failed);
		service.AvailableUpdate.CurrentValue.ShouldBeNull();
	}

	/// <summary>
	/// 非HTTPSのURLや空のリリース名に対して安全なフォールバックが行われることを確認します。
	/// </summary>
	[Fact]
	public async Task CheckForUpdatesAsync_WithUnsafeHtmlUrlAndEmptyName_UsesFallbacks() {
		// Arrange
		using var handler = CreateJsonResponseHandler("""
			{
			  "tag_name": "v1.2.0",
			  "name": "",
			  "html_url": "http://insecure.example.com/malicious",
			  "body": null,
			  "draft": false,
			  "prerelease": false
			}
			""");

		var env = new StubAppEnvironmentService(true, new Version(1, 1, 0, 0));
		using var service = new AppUpdateService(env, NullLogger<AppUpdateService>.Instance, handler, TestApiUrl);

		// Act
		var result = await service.CheckForUpdatesAsync();

		// Assert
		result.Status.ShouldBe(UpdateCheckStatus.UpdateAvailable);
		result.UpdateInfo.ShouldNotBeNull();
		result.UpdateInfo.ReleaseName.ShouldBe("v1.2.0");
		result.UpdateInfo.ReleaseNotes.ShouldBe(string.Empty);
		result.UpdateInfo.HtmlUrl.ShouldBe(AppUpdateService.FallbackReleasesPageUrl);
	}

	/// <summary>
	/// HTTPエラー、ネットワーク例外、不正なJSONの場合に <see cref="UpdateCheckStatus.Failed"/> を返しチェック中状態が解除されることを確認します。
	/// </summary>
	[Fact]
	public async Task CheckForUpdatesAsync_WhenServerErrorOrExceptionOccurs_ReturnsFailedAndResetsIsChecking() {
		// Arrange: HTTP 500
		using var errorHandler = new StubHttpMessageHandler((_, _) => {
			return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError));
		});
		var env = new StubAppEnvironmentService(true, new Version(1, 1, 0, 0));
		using var errorService = new AppUpdateService(env, NullLogger<AppUpdateService>.Instance, errorHandler, TestApiUrl);

		// Act
		var errorResult = await errorService.CheckForUpdatesAsync();

		// Assert
		errorResult.Status.ShouldBe(UpdateCheckStatus.Failed);
		errorService.IsChecking.CurrentValue.ShouldBeFalse();

		// Arrange: HttpRequestException
		using var throwingHandler = new StubHttpMessageHandler((_, _) => {
			throw new HttpRequestException("Network failure");
		});
		using var throwingService = new AppUpdateService(env, NullLogger<AppUpdateService>.Instance, throwingHandler, TestApiUrl);

		// Act
		var throwingResult = await throwingService.CheckForUpdatesAsync();

		// Assert
		throwingResult.Status.ShouldBe(UpdateCheckStatus.Failed);
		throwingService.IsChecking.CurrentValue.ShouldBeFalse();

		// Arrange: Invalid JSON
		using var invalidJsonHandler = CreateJsonResponseHandler("{ invalid json }");
		using var invalidJsonService = new AppUpdateService(env, NullLogger<AppUpdateService>.Instance, invalidJsonHandler, TestApiUrl);

		// Act
		var invalidJsonResult = await invalidJsonService.CheckForUpdatesAsync();

		// Assert
		invalidJsonResult.Status.ShouldBe(UpdateCheckStatus.Failed);
		invalidJsonService.IsChecking.CurrentValue.ShouldBeFalse();
	}

	/// <summary>
	/// キャンセル時に <see cref="OperationCanceledException"/> が伝播し、<see cref="AppUpdateService.IsChecking"/> が解除されることを確認します。
	/// </summary>
	[Fact]
	public async Task CheckForUpdatesAsync_WhenCanceled_ThrowsOperationCanceledExceptionAndResetsIsChecking() {
		// Arrange
		using var cts = new CancellationTokenSource();
		using var handler = new StubHttpMessageHandler((_, ct) => {
			cts.Cancel();
			ct.ThrowIfCancellationRequested();
			return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
		});

		var env = new StubAppEnvironmentService(true, new Version(1, 1, 0, 0));
		using var service = new AppUpdateService(env, NullLogger<AppUpdateService>.Instance, handler, TestApiUrl);

		// Act & Assert
		await Should.ThrowAsync<OperationCanceledException>(() => service.CheckForUpdatesAsync(cts.Token));
		service.IsChecking.CurrentValue.ShouldBeFalse();
	}

	/// <summary>
	/// <see cref="AppUpdateService.TryParseVersion"/> が各種タグ形式を正しく解析・除外することを確認します。
	/// </summary>
	[Theory]
	[InlineData("v1.2.0", true, "1.2.0.0")]
	[InlineData("V1.2.3.4", true, "1.2.3.4")]
	[InlineData("1.2", true, "1.2.0.0")]
	[InlineData("v1.2.0+build5", true, "1.2.0.0")]
	[InlineData("v1.2.0-beta.1", false, null)]
	[InlineData("", false, null)]
	[InlineData(null, false, null)]
	[InlineData("invalid", false, null)]
	public void TryParseVersion_ParsesExpectedFormats(string? tagName, bool expectedSuccess, string? expectedNormalized) {
		// Act
		var success = AppUpdateService.TryParseVersion(tagName, out var version);

		// Assert
		success.ShouldBe(expectedSuccess);
		if (expectedSuccess) {
			version.ShouldNotBeNull();
			version.ToString().ShouldBe(expectedNormalized);
		} else {
			version.ShouldBeNull();
		}
	}

	/// <summary>
	/// JSON文字列をHTTP 200で返すスタブハンドラーを生成します。
	/// </summary>
	/// <param name="json">レスポンスJSON</param>
	/// <returns>スタブ <see cref="HttpMessageHandler"/></returns>
	private static StubHttpMessageHandler CreateJsonResponseHandler(string json) {
		return new StubHttpMessageHandler((_, _) => {
			return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) {
				Content = new StringContent(json, Encoding.UTF8, "application/json")
			});
		});
	}

	/// <summary>
	/// 単体テスト用の <see cref="IAppEnvironmentService"/> スタブ
	/// </summary>
	private sealed class StubAppEnvironmentService : IAppEnvironmentService {
		/// <summary>
		/// <see cref="StubAppEnvironmentService"/> クラスの新しいインスタンスを初期化します。
		/// </summary>
		/// <param name="isUnpackaged">Unpackaged版かどうか</param>
		/// <param name="currentVersion">現在のバージョン</param>
		public StubAppEnvironmentService(bool isUnpackaged, Version currentVersion) {
			this.IsUnpackaged = isUnpackaged;
			this.CurrentVersion = currentVersion;
		}

		/// <inheritdoc />
		public bool IsUnpackaged {
			get;
		}

		/// <inheritdoc />
		public Version CurrentVersion {
			get;
		}
	}

	/// <summary>
	/// 単体テスト用のスタブ <see cref="HttpMessageHandler"/>
	/// </summary>
	private sealed class StubHttpMessageHandler : HttpMessageHandler {
		private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handler;

		/// <summary>
		/// <see cref="StubHttpMessageHandler"/> クラスの新しいインスタンスを初期化します。
		/// </summary>
		/// <param name="handler">リクエスト処理デリゲート</param>
		public StubHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler) {
			this._handler = handler;
		}

		/// <inheritdoc />
		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
			return this._handler(request, cancellationToken);
		}
	}
}