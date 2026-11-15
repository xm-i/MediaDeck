using System.Globalization;
using MediaDeck.Common.Base;
using MediaDeck.Common.Utilities;
using MediaDeck.Composition.Interfaces;
using MediaDeck.Composition.Interfaces.Services;
using MediaDeck.Composition.Objects;
using MediaDeck.Core.Services.Update;

namespace MediaDeck.ViewModels.Help;

/// <summary>
/// 新バージョンの更新通知ウィンドウのViewModel
/// </summary>
[Inject(InjectServiceLifetime.Transient)]
public class UpdateNotificationWindowViewModel : ViewModelBase {
	private readonly IStringProvider _stringProvider;
	private string _releaseUrl = AppUpdateService.FallbackReleasesPageUrl;

	/// <summary>
	/// <see cref="UpdateNotificationWindowViewModel"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	/// <param name="appUpdateService">アプリケーション更新サービス</param>
	/// <param name="appEnvironmentService">アプリケーション環境サービス</param>
	/// <param name="stringProvider">文字列プロバイダー</param>
	public UpdateNotificationWindowViewModel(
		IAppUpdateService appUpdateService,
		IAppEnvironmentService appEnvironmentService,
		IStringProvider stringProvider) {
		this._stringProvider = stringProvider;

		var fallbackCurrentVersion = AppUpdateService.FormatVersion(appEnvironmentService.CurrentVersion);
		this.VersionTransitionText = new BindableReactiveProperty<string>(fallbackCurrentVersion).AddTo(this.CompositeDisposable);
		this.ReleaseName = new BindableReactiveProperty<string>(string.Empty).AddTo(this.CompositeDisposable);
		this.HasReleaseName = new BindableReactiveProperty<bool>(false).AddTo(this.CompositeDisposable);
		this.PublishedAtText = new BindableReactiveProperty<string>(string.Empty).AddTo(this.CompositeDisposable);
		this.HasPublishedAt = new BindableReactiveProperty<bool>(false).AddTo(this.CompositeDisposable);
		this.ReleaseNotes = new BindableReactiveProperty<string>(stringProvider.GetString("UpdateNotification_NoReleaseNotes")).AddTo(this.CompositeDisposable);

		this.OpenReleasePageCommand = new ReactiveCommand().AddTo(this.CompositeDisposable);
		this.OpenReleasePageCommand
			.Subscribe(_ => ShellUtility.ShellExecute(this._releaseUrl))
			.AddTo(this.CompositeDisposable);

		this.CloseCommand = new ReactiveCommand().AddTo(this.CompositeDisposable);
		this.CloseCommand
			.Subscribe(_ => this.Close())
			.AddTo(this.CompositeDisposable);

		this.ApplyUpdateInfo(appUpdateService.AvailableUpdate.CurrentValue);

		var updateObservable = appUpdateService.AvailableUpdate.AsObservable();
		if (SynchronizationContext.Current != null) {
			updateObservable = updateObservable.ObserveOnCurrentSynchronizationContext();
		}

		updateObservable
			.Subscribe(this.ApplyUpdateInfo)
			.AddTo(this.CompositeDisposable);
	}

	/// <summary>
	/// バージョン移行の表示テキスト（例: v1.1.0 → v1.2.0）
	/// </summary>
	public BindableReactiveProperty<string> VersionTransitionText {
		get;
	}

	/// <summary>
	/// リリース名
	/// </summary>
	public BindableReactiveProperty<string> ReleaseName {
		get;
	}

	/// <summary>
	/// リリース名が表示可能かどうか
	/// </summary>
	public BindableReactiveProperty<bool> HasReleaseName {
		get;
	}

	/// <summary>
	/// 公開日時の表示テキスト
	/// </summary>
	public BindableReactiveProperty<string> PublishedAtText {
		get;
	}

	/// <summary>
	/// 公開日時が表示可能かどうか
	/// </summary>
	public BindableReactiveProperty<bool> HasPublishedAt {
		get;
	}

	/// <summary>
	/// リリースノート本文
	/// </summary>
	public BindableReactiveProperty<string> ReleaseNotes {
		get;
	}

	/// <summary>
	/// リリースページをブラウザで開くコマンド
	/// </summary>
	public ReactiveCommand OpenReleasePageCommand {
		get;
	}

	/// <summary>
	/// ウィンドウを閉じるコマンド
	/// </summary>
	public ReactiveCommand CloseCommand {
		get;
	}

	/// <summary>
	/// 取得された更新情報をViewModelのプロパティへ反映します。
	/// </summary>
	/// <param name="updateInfo">適用する更新情報</param>
	private void ApplyUpdateInfo(AppUpdateInfo? updateInfo) {
		if (updateInfo == null) {
			return;
		}

		this._releaseUrl = updateInfo.HtmlUrl;
		this.VersionTransitionText.Value = this._stringProvider.GetString(
			"UpdateNotification_VersionTransitionFormat",
			updateInfo.CurrentVersionText,
			updateInfo.VersionText);
		this.ReleaseName.Value = updateInfo.ReleaseName;
		this.HasReleaseName.Value = !string.IsNullOrWhiteSpace(updateInfo.ReleaseName);

		if (updateInfo.PublishedAt.HasValue) {
			var dateText = updateInfo.PublishedAt.Value.ToLocalTime().ToString("d", CultureInfo.CurrentCulture);
			this.PublishedAtText.Value = this._stringProvider.GetString("UpdateNotification_PublishedAtFormat", dateText);
			this.HasPublishedAt.Value = true;
		} else {
			this.PublishedAtText.Value = string.Empty;
			this.HasPublishedAt.Value = false;
		}

		this.ReleaseNotes.Value = string.IsNullOrWhiteSpace(updateInfo.ReleaseNotes)
			? this._stringProvider.GetString("UpdateNotification_NoReleaseNotes")
			: updateInfo.ReleaseNotes;
	}
}