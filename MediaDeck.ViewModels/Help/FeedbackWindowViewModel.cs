using MediaDeck.Common.Base;
using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces;
using MediaDeck.Composition.Interfaces.Services;
using MediaDeck.Composition.Objects;
using MediaDeck.Core.Primitives;

namespace MediaDeck.ViewModels.Help;

/// <summary>
/// フィードバックウィンドウのViewModel
/// </summary>
[Inject(InjectServiceLifetime.Transient)]
public class FeedbackWindowViewModel : ViewModelBase {
	private readonly IFeedbackService _feedbackService;
	private readonly IFilePickerService _filePickerService;
	private readonly IAppNotificationService _appNotificationService;
	private readonly IStringProvider _stringProvider;

	/// <summary>
	/// <see cref="FeedbackWindowViewModel"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	/// <param name="feedbackService">フィードバックサービス</param>
	/// <param name="filePickerService">ファイルピッカーサービス</param>
	/// <param name="appNotificationService">アプリケーション通知サービス</param>
	/// <param name="stringProvider">文字列プロバイダー</param>
	public FeedbackWindowViewModel(
		IFeedbackService feedbackService,
		IFilePickerService filePickerService,
		IAppNotificationService appNotificationService,
		IStringProvider stringProvider) {
		this._feedbackService = feedbackService;
		this._filePickerService = filePickerService;
		this._appNotificationService = appNotificationService;
		this._stringProvider = stringProvider;

		this.Categories = [
			new DisplayObject<FeedbackCategory>(stringProvider.GetString("Feedback_Category_BugReport"), FeedbackCategory.BugReport),
			new DisplayObject<FeedbackCategory>(stringProvider.GetString("Feedback_Category_FeatureRequest"), FeedbackCategory.FeatureRequest),
			new DisplayObject<FeedbackCategory>(stringProvider.GetString("Feedback_Category_Other"), FeedbackCategory.Other)
		];

		this.SelectedCategory = new BindableReactiveProperty<DisplayObject<FeedbackCategory>>(this.Categories[0]).AddTo(this.CompositeDisposable);
		this.Title = new BindableReactiveProperty<string>(string.Empty).AddTo(this.CompositeDisposable);
		this.Description = new BindableReactiveProperty<string>(string.Empty).AddTo(this.CompositeDisposable);
		this.Contact = new BindableReactiveProperty<string>(string.Empty).AddTo(this.CompositeDisposable);
		this.IncludeEnvironmentInfo = new BindableReactiveProperty<bool>(true).AddTo(this.CompositeDisposable);
		this.EnvironmentInfo = feedbackService.GetEnvironmentInfo();
		this.IncludeLogs = new BindableReactiveProperty<bool>(true).AddTo(this.CompositeDisposable);
		this.LogPreview = new BindableReactiveProperty<string>(string.Empty).AddTo(this.CompositeDisposable);
		this.ScreenshotBytes = new BindableReactiveProperty<byte[]?>(null).AddTo(this.CompositeDisposable);
		this.HasScreenshot = this.ScreenshotBytes
			.Select(bytes => bytes is { Length: > 0 })
			.ToBindableReactiveProperty(false)
			.AddTo(this.CompositeDisposable);
		this.IsSubmitting = new BindableReactiveProperty<bool>(false).AddTo(this.CompositeDisposable);
		this.ErrorMessage = new BindableReactiveProperty<string>(string.Empty).AddTo(this.CompositeDisposable);
		this.HasError = this.ErrorMessage
			.Select(message => !string.IsNullOrEmpty(message))
			.ToBindableReactiveProperty(false)
			.AddTo(this.CompositeDisposable);

		this.CanSubmit = Observable.CombineLatest(
			this.Title,
			this.Description,
			this.IsSubmitting,
			(title, description, isSubmitting) => {
				return (!isSubmitting)
					&& (!string.IsNullOrWhiteSpace(title))
					&& (!string.IsNullOrWhiteSpace(description));
			})
			.ToBindableReactiveProperty(false)
			.AddTo(this.CompositeDisposable);

		this.PickScreenshotCommand = this.IsSubmitting
			.Select(isSubmitting => !isSubmitting)
			.ToReactiveCommand()
			.AddTo(this.CompositeDisposable);
		this.PickScreenshotCommand.SubscribeAwait(async (_, ct) => {
			var bytes = await this._filePickerService.PickImageAsync();
			if ((this.DisposeState == DisposeState.NotDisposed) && (bytes is { Length: > 0 })) {
				this.ScreenshotBytes.Value = bytes;
			}
		}, AwaitOperation.Drop).AddTo(this.CompositeDisposable);

		this.ClearScreenshotCommand = Observable.CombineLatest(
			this.HasScreenshot,
			this.IsSubmitting,
			(hasScreenshot, isSubmitting) => {
				return hasScreenshot && (!isSubmitting);
			})
			.ToReactiveCommand()
			.AddTo(this.CompositeDisposable);
		this.ClearScreenshotCommand.Subscribe(_ => {
			this.ScreenshotBytes.Value = null;
		}).AddTo(this.CompositeDisposable);

		this.RefreshLogsCommand = this.IsSubmitting
			.Select(isSubmitting => !isSubmitting)
			.ToReactiveCommand()
			.AddTo(this.CompositeDisposable);
		this.RefreshLogsCommand.SubscribeAwait(async (_, ct) => {
			var logs = await this._feedbackService.GetRecentLogsAsync(cancellationToken: ct);
			if (this.DisposeState == DisposeState.NotDisposed) {
				this.LogPreview.Value = logs;
			}
		}, AwaitOperation.Drop).AddTo(this.CompositeDisposable);

		this.SubmitCommand = this.CanSubmit
			.ToReactiveCommand()
			.AddTo(this.CompositeDisposable);
		this.SubmitCommand.SubscribeAwait(async (_, ct) => {
			await this.SubmitAsync(ct);
		}, AwaitOperation.Drop).AddTo(this.CompositeDisposable);

		this.CancelCommand = new ReactiveCommand().AddTo(this.CompositeDisposable);
		this.CancelCommand.Subscribe(_ => {
			this.Close();
		}).AddTo(this.CompositeDisposable);

		this.RefreshLogsCommand.Execute(Unit.Default);
	}

	/// <summary>
	/// 選択可能なフィードバック種別一覧
	/// </summary>
	public IReadOnlyList<DisplayObject<FeedbackCategory>> Categories {
		get;
	}

	/// <summary>
	/// 選択中のフィードバック種別
	/// </summary>
	public BindableReactiveProperty<DisplayObject<FeedbackCategory>> SelectedCategory {
		get;
	}

	/// <summary>
	/// 件名
	/// </summary>
	public BindableReactiveProperty<string> Title {
		get;
	}

	/// <summary>
	/// 詳細内容
	/// </summary>
	public BindableReactiveProperty<string> Description {
		get;
	}

	/// <summary>
	/// 連絡先（任意）
	/// </summary>
	public BindableReactiveProperty<string> Contact {
		get;
	}

	/// <summary>
	/// 環境情報を添付するかどうか
	/// </summary>
	public BindableReactiveProperty<bool> IncludeEnvironmentInfo {
		get;
	}

	/// <summary>
	/// 取得された環境情報テキスト
	/// </summary>
	public string EnvironmentInfo {
		get;
	}

	/// <summary>
	/// 直近のログを添付するかどうか
	/// </summary>
	public BindableReactiveProperty<bool> IncludeLogs {
		get;
	}

	/// <summary>
	/// 直近ログのプレビューテキスト
	/// </summary>
	public BindableReactiveProperty<string> LogPreview {
		get;
	}

	/// <summary>
	/// 添付スクリーンショットのバイト配列
	/// </summary>
	public BindableReactiveProperty<byte[]?> ScreenshotBytes {
		get;
	}

	/// <summary>
	/// スクリーンショットが添付されているかどうか
	/// </summary>
	public BindableReactiveProperty<bool> HasScreenshot {
		get;
	}

	/// <summary>
	/// 送信処理中かどうか
	/// </summary>
	public BindableReactiveProperty<bool> IsSubmitting {
		get;
	}

	/// <summary>
	/// エラーメッセージ
	/// </summary>
	public BindableReactiveProperty<string> ErrorMessage {
		get;
	}

	/// <summary>
	/// エラーが発生しているかどうか
	/// </summary>
	public BindableReactiveProperty<bool> HasError {
		get;
	}

	/// <summary>
	/// 送信可能かどうか
	/// </summary>
	public BindableReactiveProperty<bool> CanSubmit {
		get;
	}

	/// <summary>
	/// スクリーンショット画像を選択するコマンド
	/// </summary>
	public ReactiveCommand PickScreenshotCommand {
		get;
	}

	/// <summary>
	/// 添付スクリーンショットをクリアするコマンド
	/// </summary>
	public ReactiveCommand ClearScreenshotCommand {
		get;
	}

	/// <summary>
	/// 直近ログを再読み込みするコマンド
	/// </summary>
	public ReactiveCommand RefreshLogsCommand {
		get;
	}

	/// <summary>
	/// フィードバックを送信するコマンド
	/// </summary>
	public ReactiveCommand SubmitCommand {
		get;
	}

	/// <summary>
	/// ウィンドウを閉じてキャンセルするコマンド
	/// </summary>
	public ReactiveCommand CancelCommand {
		get;
	}

	/// <summary>
	/// フィードバックを送信します。
	/// </summary>
	/// <param name="cancellationToken">キャンセルトークン</param>
	private async Task SubmitAsync(CancellationToken cancellationToken) {
		this.ErrorMessage.Value = string.Empty;
		this.IsSubmitting.Value = true;

		try {
			var screenshot = this.ScreenshotBytes.Value;
			var contactText = this.Contact.Value.Trim();
			var payload = new FeedbackPayload {
				Category = this.SelectedCategory.Value.Value.ToString(),
				Title = this.Title.Value.Trim(),
				Description = this.Description.Value.Trim(),
				Contact = string.IsNullOrEmpty(contactText) ? null : contactText,
				EnvironmentInfo = this.IncludeEnvironmentInfo.Value ? this.EnvironmentInfo : null,
				LogContent = (this.IncludeLogs.Value && (!string.IsNullOrWhiteSpace(this.LogPreview.Value))) ? this.LogPreview.Value : null,
				ScreenshotBase64 = screenshot is { Length: > 0 } ? Convert.ToBase64String(screenshot) : null,
				SubmittedAt = DateTimeOffset.UtcNow
			};

			var success = await this._feedbackService.SendFeedbackAsync(payload, cancellationToken);
			if (this.DisposeState != DisposeState.NotDisposed) {
				return;
			}

			if (success) {
				this._appNotificationService.Notify(
					AppNotification.Success(
						this._stringProvider.GetString("Feedback_SubmitSuccess_Message"),
						this._stringProvider.GetString("Feedback_SubmitSuccess_Title")));
				this.Close();
			} else {
				this.ErrorMessage.Value = this._stringProvider.GetString("Feedback_SubmitError_Message");
			}
		} catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
			// キャンセル時は何もしない
		} catch {
			if (this.DisposeState == DisposeState.NotDisposed) {
				this.ErrorMessage.Value = this._stringProvider.GetString("Feedback_SubmitError_Message");
			}
		} finally {
			if (this.DisposeState == DisposeState.NotDisposed) {
				this.IsSubmitting.Value = false;
			}
		}
	}
}