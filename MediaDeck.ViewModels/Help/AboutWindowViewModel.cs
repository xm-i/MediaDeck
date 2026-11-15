using MediaDeck.Common.Base;
using MediaDeck.Common.Utilities;
using MediaDeck.Composition.Interfaces;
using MediaDeck.Composition.Interfaces.Services;
using MediaDeck.Composition.Objects;

namespace MediaDeck.ViewModels.Help;

/// <summary>
/// AboutウィンドウのViewModel
/// </summary>
[Inject(InjectServiceLifetime.Transient)]
public class AboutWindowViewModel : ViewModelBase {
	private readonly IAppUpdateService _appUpdateService;
	private readonly IStringProvider _stringProvider;

	/// <summary>
	/// <see cref="AboutWindowViewModel"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	/// <param name="stringProvider">文字列プロバイダー</param>
	/// <param name="appPathProvider">アプリケーションパスプロバイダー</param>
	/// <param name="appEnvironmentService">アプリケーション環境サービス</param>
	/// <param name="appUpdateService">アプリケーション更新サービス</param>
	public AboutWindowViewModel(
		IStringProvider stringProvider,
		IAppPathProvider appPathProvider,
		IAppEnvironmentService appEnvironmentService,
		IAppUpdateService appUpdateService) {
		this._stringProvider = stringProvider;
		this._appUpdateService = appUpdateService;

		this.AppName = stringProvider.GetString("App_Title");
		this.Version = appEnvironmentService.CurrentVersion.ToString();
		this.Description = stringProvider.GetString("About_Description");
		this.Copyright = stringProvider.GetString("About_Copyright");
		this.AppDataDirectoryPath = appPathProvider.BaseDirectory;
		this.IsUpdateCheckSupported = appEnvironmentService.IsUnpackaged;

		this.OpenAppDataDirectoryCommand = new ReactiveCommand().AddTo(this.CompositeDisposable);
		this.OpenAppDataDirectoryCommand
			.Subscribe(_ => ShellUtility.ShellExecute(this.AppDataDirectoryPath))
			.AddTo(this.CompositeDisposable);

		this.OpenWebSiteCommand = new ReactiveCommand().AddTo(this.CompositeDisposable);
		this.OpenWebSiteCommand
			.Subscribe(_ => ShellUtility.ShellExecute("https://mediadeck.xm-i.net/"))
			.AddTo(this.CompositeDisposable);

		var isCheckingObservable = appUpdateService.IsChecking.AsObservable();
		if (SynchronizationContext.Current != null) {
			isCheckingObservable = isCheckingObservable.ObserveOnCurrentSynchronizationContext();
		}

		this.IsCheckingUpdate = isCheckingObservable
			.ToBindableReactiveProperty(appUpdateService.IsChecking.CurrentValue)
			.AddTo(this.CompositeDisposable);

		this.HasAvailableUpdate = new BindableReactiveProperty<bool>(false).AddTo(this.CompositeDisposable);
		var initialMessageKey = appUpdateService.IsChecking.CurrentValue
			? "About_UpdateStatus_Checking"
			: "About_UpdateStatus_Initial";
		this.UpdateStatusMessage = new BindableReactiveProperty<string>(stringProvider.GetString(initialMessageKey))
			.AddTo(this.CompositeDisposable);

		this.ApplyAvailableUpdate(appUpdateService.AvailableUpdate.CurrentValue);

		var availableUpdateObservable = appUpdateService.AvailableUpdate.AsObservable();
		if (SynchronizationContext.Current != null) {
			availableUpdateObservable = availableUpdateObservable.ObserveOnCurrentSynchronizationContext();
		}

		availableUpdateObservable
			.Subscribe(this.ApplyAvailableUpdate)
			.AddTo(this.CompositeDisposable);

		this.CheckForUpdatesCommand = this.IsCheckingUpdate
			.Select(isChecking => !isChecking)
			.ToReactiveCommand()
			.AddTo(this.CompositeDisposable);

		this.CheckForUpdatesCommand
			.SubscribeAwait(async (_, ct) => {
				await this.CheckForUpdatesAsync(ct);
			}, AwaitOperation.Drop)
			.AddTo(this.CompositeDisposable);
	}

	/// <summary>
	/// アプリケーション名
	/// </summary>
	public string AppName {
		get;
	}

	/// <summary>
	/// バージョン
	/// </summary>
	public string Version {
		get;
	}

	/// <summary>
	/// 説明
	/// </summary>
	public string Description {
		get;
	}

	/// <summary>
	/// 著作権情報
	/// </summary>
	public string Copyright {
		get;
	}

	/// <summary>
	/// アプリケーションデータフォルダのパス
	/// </summary>
	public string AppDataDirectoryPath {
		get;
	}

	/// <summary>
	/// アプリ内での更新確認に対応しているかどうか（Unpackaged版のみ true）
	/// </summary>
	public bool IsUpdateCheckSupported {
		get;
	}

	/// <summary>
	/// アップデート確認中かどうか
	/// </summary>
	public BindableReactiveProperty<bool> IsCheckingUpdate {
		get;
	}

	/// <summary>
	/// 利用可能なアップデートが存在するかどうか
	/// </summary>
	public BindableReactiveProperty<bool> HasAvailableUpdate {
		get;
	}

	/// <summary>
	/// アップデート確認結果のステータスメッセージ
	/// </summary>
	public BindableReactiveProperty<string> UpdateStatusMessage {
		get;
	}

	/// <summary>
	/// アプリケーションデータフォルダをエクスプローラーで開くコマンド
	/// </summary>
	public ReactiveCommand OpenAppDataDirectoryCommand {
		get;
	}

	/// <summary>
	/// 公式ウェブサイトをブラウザで開くコマンド
	/// </summary>
	public ReactiveCommand OpenWebSiteCommand {
		get;
	}

	/// <summary>
	/// 手動でアップデートの確認を実行するコマンド
	/// </summary>
	public ReactiveCommand CheckForUpdatesCommand {
		get;
	}

	/// <summary>
	/// 更新確認を実行し、結果メッセージを更新します。
	/// </summary>
	/// <param name="cancellationToken">キャンセルトークン</param>
	private async Task CheckForUpdatesAsync(CancellationToken cancellationToken) {
		this.UpdateStatusMessage.Value = this._stringProvider.GetString("About_UpdateStatus_Checking");

		var result = await this._appUpdateService.CheckForUpdatesAsync(cancellationToken);
		if (this.DisposeState != Composition.Enum.DisposeState.NotDisposed) {
			return;
		}

		this.UpdateStatusMessage.Value = result.Status switch {
			UpdateCheckStatus.UpdateAvailable when result.UpdateInfo != null =>
				this._stringProvider.GetString("About_UpdateStatus_AvailableFormat", result.UpdateInfo.VersionText),
			UpdateCheckStatus.UpToDate =>
				this._stringProvider.GetString("About_UpdateStatus_UpToDate"),
			UpdateCheckStatus.Failed =>
				this._stringProvider.GetString("About_UpdateStatus_Failed"),
			_ => string.Empty
		};
	}

	/// <summary>
	/// 更新情報の変更をプロパティへ反映します。
	/// </summary>
	/// <param name="updateInfo">利用可能な更新情報</param>
	private void ApplyAvailableUpdate(AppUpdateInfo? updateInfo) {
		if (updateInfo != null) {
			this.HasAvailableUpdate.Value = true;
			this.UpdateStatusMessage.Value = this._stringProvider.GetString("About_UpdateStatus_AvailableFormat", updateInfo.VersionText);
		} else {
			this.HasAvailableUpdate.Value = false;
		}
	}
}