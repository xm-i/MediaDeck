using System.Net;
using System.Text.Json;
using MediaDeck.Composition.Objects;
using MediaDeck.Core.Services.Feedback;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;

namespace MediaDeck.Core.Tests.Services.Feedback;

/// <summary>
/// <see cref="FeedbackService"/> の単体テストクラス
/// </summary>
public class FeedbackServiceTests : IDisposable {
	private readonly StubAppPathProvider _appPathProvider;

	/// <summary>
	/// <see cref="FeedbackServiceTests"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	public FeedbackServiceTests() {
		this._appPathProvider = new StubAppPathProvider();
	}

	/// <summary>
	/// テスト終了時に一時ディレクトリをクリーンアップします。
	/// </summary>
	public void Dispose() {
		this._appPathProvider.Cleanup();
	}

	/// <summary>
	/// <see cref="FeedbackService.GetEnvironmentInfo"/> がアプリケーションおよびOS情報を含む文字列を返すことを確認します。
	/// </summary>
	[Fact]
	public void GetEnvironmentInfo_ReturnsApplicationAndOsInformation() {
		// Arrange
		using var service = new FeedbackService(this._appPathProvider, NullLogger<FeedbackService>.Instance);

		// Act
		var info = service.GetEnvironmentInfo();

		// Assert
		info.ShouldNotBeNullOrWhiteSpace();
		info.ShouldContain("App Version:");
		info.ShouldContain("OS:");
		info.ShouldContain("OS Architecture:");
		info.ShouldContain("Process Architecture:");
		info.ShouldContain("Runtime:");
		info.ShouldContain("Culture:");
	}

	/// <summary>
	/// <see cref="FeedbackService.GetRecentLogsAsync"/> が最新のログファイルから末尾の指定行数を取得できることを確認します。
	/// </summary>
	[Fact]
	public async Task GetRecentLogsAsync_ReturnsLastNLinesFromLatestLogFile() {
		// Arrange
		var logDir = Path.Combine(this._appPathProvider.BaseDirectory, "log");
		Directory.CreateDirectory(logDir);

		var olderFile = Path.Combine(logDir, "202601.log");
		await File.WriteAllLinesAsync(olderFile, ["old line 1", "old line 2"]);
		File.SetLastWriteTimeUtc(olderFile, DateTime.UtcNow.AddDays(-1));

		var latestFile = Path.Combine(logDir, "202602.log");
		await File.WriteAllLinesAsync(latestFile, ["line 1", "line 2", "line 3", "line 4", "line 5"]);
		File.SetLastWriteTimeUtc(latestFile, DateTime.UtcNow);

		using var service = new FeedbackService(this._appPathProvider, NullLogger<FeedbackService>.Instance);

		// Act
		var logs = await service.GetRecentLogsAsync(maxLines: 3);

		// Assert
		var expected = string.Join(Environment.NewLine, ["line 3", "line 4", "line 5"]);
		logs.ShouldBe(expected);
	}

	/// <summary>
	/// <see cref="FeedbackService.GetRecentLogsAsync"/> がログディレクトリ未存在時に空文字列を返すことを確認します。
	/// </summary>
	[Fact]
	public async Task GetRecentLogsAsync_WhenLogDirectoryDoesNotExist_ReturnsEmptyString() {
		// Arrange
		using var service = new FeedbackService(this._appPathProvider, NullLogger<FeedbackService>.Instance);

		// Act
		var logs = await service.GetRecentLogsAsync();

		// Assert
		logs.ShouldBe(string.Empty);
	}

	/// <summary>
	/// <see cref="FeedbackService.GetRecentLogsAsync"/> がログファイル未存在時に空文字列を返すことを確認します。
	/// </summary>
	[Fact]
	public async Task GetRecentLogsAsync_WhenNoLogFilesExist_ReturnsEmptyString() {
		// Arrange
		var logDir = Path.Combine(this._appPathProvider.BaseDirectory, "log");
		Directory.CreateDirectory(logDir);
		using var service = new FeedbackService(this._appPathProvider, NullLogger<FeedbackService>.Instance);

		// Act
		var logs = await service.GetRecentLogsAsync();

		// Assert
		logs.ShouldBe(string.Empty);
	}

	/// <summary>
	/// <see cref="FeedbackService.SendFeedbackAsync"/> が正しいJSONをPOSTし、HTTP 200で true を返すことを確認します。
	/// </summary>
	[Fact]
	public async Task SendFeedbackAsync_PostsValidJsonAndReturnsTrueOn200() {
		// Arrange
		string? capturedBody = null;
		Uri? capturedUri = null;
		HttpMethod? capturedMethod = null;
		string? capturedContentType = null;

		using var handler = new StubHttpMessageHandler(async (request, ct) => {
			capturedMethod = request.Method;
			capturedUri = request.RequestUri;
			capturedContentType = request.Content?.Headers.ContentType?.MediaType;
			if (request.Content != null) {
				capturedBody = await request.Content.ReadAsStringAsync(ct);
			}
			return new HttpResponseMessage(HttpStatusCode.OK);
		});

		const string endpointUrl = "https://example.test/api/feedback";
		using var service = new FeedbackService(
			this._appPathProvider,
			NullLogger<FeedbackService>.Instance,
			handler,
			endpointUrl);

		var submittedAt = new DateTimeOffset(2026, 11, 14, 12, 0, 0, TimeSpan.Zero);
		var payload = new FeedbackPayload {
			Category = "BugReport",
			Title = "Test Bug",
			Description = "Something went wrong",
			Contact = null,
			EnvironmentInfo = "OS: Windows",
			LogContent = "ERR sample log",
			ScreenshotBase64 = "AQID",
			SubmittedAt = submittedAt
		};

		// Act
		var result = await service.SendFeedbackAsync(payload);

		// Assert
		result.ShouldBeTrue();
		capturedMethod.ShouldBe(HttpMethod.Post);
		capturedUri.ShouldBe(new Uri(endpointUrl));
		capturedContentType.ShouldBe("application/json");
		capturedBody.ShouldNotBeNull();

		using var doc = JsonDocument.Parse(capturedBody);
		var root = doc.RootElement;
		root.GetProperty("category").GetString().ShouldBe("BugReport");
		root.GetProperty("title").GetString().ShouldBe("Test Bug");
		root.GetProperty("description").GetString().ShouldBe("Something went wrong");
		root.TryGetProperty("contact", out _).ShouldBeFalse();
		root.GetProperty("environmentInfo").GetString().ShouldBe("OS: Windows");
		root.GetProperty("logContent").GetString().ShouldBe("ERR sample log");
		root.GetProperty("screenshotBase64").GetString().ShouldBe("AQID");
		root.GetProperty("submittedAt").GetDateTimeOffset().ShouldBe(submittedAt);
	}

	/// <summary>
	/// <see cref="FeedbackService.SendFeedbackAsync"/> がHTTP 500で false を返すことを確認します。
	/// </summary>
	[Fact]
	public async Task SendFeedbackAsync_ReturnsFalseOn500() {
		// Arrange
		using var handler = new StubHttpMessageHandler((_, _) => {
			return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError));
		});

		using var service = new FeedbackService(
			this._appPathProvider,
			NullLogger<FeedbackService>.Instance,
			handler,
			"https://example.test/api/feedback");

		var payload = new FeedbackPayload {
			Category = "FeatureRequest",
			Title = "Feature",
			Description = "Add dark mode",
			SubmittedAt = DateTimeOffset.UtcNow
		};

		// Act
		var result = await service.SendFeedbackAsync(payload);

		// Assert
		result.ShouldBeFalse();
	}

	/// <summary>
	/// <see cref="FeedbackService.SendFeedbackAsync"/> がHTTP例外発生時に false を返すことを確認します。
	/// </summary>
	[Fact]
	public async Task SendFeedbackAsync_ReturnsFalseWhenRequestThrows() {
		// Arrange
		using var handler = new StubHttpMessageHandler((_, _) => {
			throw new HttpRequestException("Network error");
		});

		using var service = new FeedbackService(
			this._appPathProvider,
			NullLogger<FeedbackService>.Instance,
			handler,
			"https://example.test/api/feedback");

		var payload = new FeedbackPayload {
			Category = "Other",
			Title = "Question",
			Description = "How does this work?",
			SubmittedAt = DateTimeOffset.UtcNow
		};

		// Act
		var result = await service.SendFeedbackAsync(payload);

		// Assert
		result.ShouldBeFalse();
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