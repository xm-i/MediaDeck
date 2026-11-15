using MediaDeck.Common.Base;
using MediaDeck.Composition.Interfaces;
using MediaDeck.Composition.Interfaces.Services;
using MediaDeck.Core.Services.FileChangeMonitor;

namespace MediaDeck.ViewModels;

/// <summary>
/// ナビゲーションメニューのViewModel
/// </summary>
[Inject(InjectServiceLifetime.Scoped)]
public class NavigationMenuViewModel : ViewModelBase {
	private readonly IStringProvider _stringProvider;

	/// <summary>
	/// 未処理のファイル変更が存在するかどうか
	/// </summary>
	public BindableReactiveProperty<bool> HasUnprocessedChanges {
		get;
	}

	/// <summary>
	/// 利用可能なアプリケーション更新が存在するかどうか
	/// </summary>
	public BindableReactiveProperty<bool> HasAvailableUpdate {
		get;
	}

	/// <summary>
	/// 更新通知ボタンに表示するラベルテキスト
	/// </summary>
	public BindableReactiveProperty<string> UpdateNotificationLabel {
		get;
	}

	/// <summary>
	/// 更新通知ボタンのツールチップテキスト
	/// </summary>
	public BindableReactiveProperty<string> UpdateNotificationToolTip {
		get;
	}

	/// <summary>
	/// <see cref="NavigationMenuViewModel"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	/// <param name="fileChangeMonitorService">ファイル変更監視サービス</param>
	/// <param name="appUpdateService">アプリケーション更新サービス</param>
	/// <param name="stringProvider">文字列プロバイダー</param>
	public NavigationMenuViewModel(
		FileChangeMonitorService fileChangeMonitorService,
		IAppUpdateService appUpdateService,
		IStringProvider stringProvider) {
		this._stringProvider = stringProvider;

		var unprocessedChangesObservable = fileChangeMonitorService.Tracker.UnprocessedChanges.ObserveCountChanged();
		if (SynchronizationContext.Current != null) {
			unprocessedChangesObservable = unprocessedChangesObservable.ObserveOnCurrentSynchronizationContext();
		}

		this.HasUnprocessedChanges = unprocessedChangesObservable
			.Select(count => count > 0)
			.ToBindableReactiveProperty(fileChangeMonitorService.Tracker.UnprocessedChanges.Count > 0)
			.AddTo(this.CompositeDisposable);

		this.HasAvailableUpdate = new BindableReactiveProperty<bool>(false).AddTo(this.CompositeDisposable);
		this.UpdateNotificationLabel = new BindableReactiveProperty<string>(string.Empty).AddTo(this.CompositeDisposable);
		this.UpdateNotificationToolTip = new BindableReactiveProperty<string>(string.Empty).AddTo(this.CompositeDisposable);

		this.ApplyAvailableUpdate(appUpdateService.AvailableUpdate.CurrentValue);

		var updateObservable = appUpdateService.AvailableUpdate.AsObservable();
		if (SynchronizationContext.Current != null) {
			updateObservable = updateObservable.ObserveOnCurrentSynchronizationContext();
		}

		updateObservable
			.Subscribe(this.ApplyAvailableUpdate)
			.AddTo(this.CompositeDisposable);
	}

	/// <summary>
	/// 更新情報をナビゲーションメニューの通知プロパティへ反映します。
	/// </summary>
	/// <param name="updateInfo">更新情報</param>
	private void ApplyAvailableUpdate(Composition.Objects.AppUpdateInfo? updateInfo) {
		if (updateInfo != null) {
			this.HasAvailableUpdate.Value = true;
			this.UpdateNotificationLabel.Value = this._stringProvider.GetString("NavMenu_UpdateAvailableFormat", updateInfo.VersionText);
			this.UpdateNotificationToolTip.Value = this._stringProvider.GetString("NavMenu_UpdateNotification_ToolTipFormat", updateInfo.VersionText);
		} else {
			this.HasAvailableUpdate.Value = false;
			this.UpdateNotificationLabel.Value = string.Empty;
			this.UpdateNotificationToolTip.Value = string.Empty;
		}
	}
}