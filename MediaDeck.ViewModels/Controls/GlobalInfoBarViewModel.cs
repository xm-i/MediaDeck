using System.Collections.Concurrent;
using MediaDeck.Common.Base;
using MediaDeck.Composition.Objects;
using MediaDeck.Core.Models.NotificationDispatcher;

namespace MediaDeck.ViewModels.Controls;

/// <summary>
/// アプリケーション全体の通知を InfoBar で表示するための ViewModel。
/// </summary>
[Inject(InjectServiceLifetime.Scoped)]
public class GlobalInfoBarViewModel : ViewModelBase {
	private readonly ConcurrentQueue<InfoBarNotification> _notificationQueue = new();
	private readonly SerialDisposable _autoCloseTimer = new();
	private readonly Guid? _windowId;
	private bool _isShowingNotification;

	/// <summary>
	/// InfoBar が開いているかどうか
	/// </summary>
	public BindableReactiveProperty<bool> IsOpen {
		get;
	}

	/// <summary>
	/// 表示中の通知タイトル
	/// </summary>
	public BindableReactiveProperty<string?> Title {
		get;
	}

	/// <summary>
	/// 表示中の通知メッセージ
	/// </summary>
	public BindableReactiveProperty<string> Message {
		get;
	}

	/// <summary>
	/// 表示中の通知の重要度
	/// </summary>
	public BindableReactiveProperty<NotificationSeverity> Severity {
		get;
	}

	/// <summary>
	/// InfoBar が閉じられたときのコマンド
	/// </summary>
	public ReactiveCommand ClosedCommand {
		get;
	}

	/// <summary>
	/// GlobalInfoBarViewModel の新しいインスタンスを初期化します。
	/// </summary>
	/// <param name="dispatcher">アプリケーション通知ディスパッチャー</param>
	/// <param name="windowStateProvider">ウィンドウ状態プロバイダー</param>
	public GlobalInfoBarViewModel(AppNotificationDispatcher dispatcher, WindowStateProvider windowStateProvider) {
		this._windowId = windowStateProvider.State?.WindowId;

		this.IsOpen = new BindableReactiveProperty<bool>(false).AddTo(this.CompositeDisposable);
		this.Title = new BindableReactiveProperty<string?>().AddTo(this.CompositeDisposable);
		this.Message = new BindableReactiveProperty<string>(string.Empty).AddTo(this.CompositeDisposable);
		this.Severity = new BindableReactiveProperty<NotificationSeverity>(NotificationSeverity.Informational).AddTo(this.CompositeDisposable);

		this._autoCloseTimer.AddTo(this.CompositeDisposable);

		this.ClosedCommand = new ReactiveCommand().AddTo(this.CompositeDisposable);
		this.ClosedCommand.Subscribe(_ => this.OnClosed()).AddTo(this.CompositeDisposable);

		var notifyObservable = dispatcher.Notify.AsObservable();
		if (SynchronizationContext.Current != null) {
			notifyObservable = notifyObservable.ObserveOnCurrentSynchronizationContext();
		}

		notifyObservable
			.Where(appNotification => {
				// ブロードキャスト通知なら表示
				if (appNotification.TargetWindowId == null) {
					return true;
				}

				// 自身の Window への通知なら表示
				return this._windowId != null && appNotification.TargetWindowId == this._windowId;
			})
			.Subscribe(appNotification => {
				var infoBarNotification = InfoBarNotification.FromAppNotification(appNotification);
				this.EnqueueNotification(infoBarNotification);
			})
			.AddTo(this.CompositeDisposable);
	}

	/// <summary>
	/// 通知をキューに追加し、表示処理を開始します。
	/// </summary>
	/// <param name="notification">表示する通知</param>
	public void EnqueueNotification(InfoBarNotification notification) {
		this._notificationQueue.Enqueue(notification);
		this.ShowNextNotification();
	}

	/// <summary>
	/// キューから次の通知を取り出して表示します。
	/// </summary>
	private void ShowNextNotification() {
		if (this._isShowingNotification) {
			return;
		}

		if (!this._notificationQueue.TryDequeue(out var notification)) {
			return;
		}

		this._isShowingNotification = true;
		this._autoCloseTimer.Disposable = null;

		this.Title.Value = notification.Title;
		this.Message.Value = notification.Message;
		this.Severity.Value = notification.Severity;
		this.IsOpen.Value = true;

		if (notification.AutoCloseMilliseconds > 0) {
			var timerObservable = Observable.Timer(TimeSpan.FromMilliseconds(notification.AutoCloseMilliseconds));
			if (SynchronizationContext.Current != null) {
				timerObservable = timerObservable.ObserveOnCurrentSynchronizationContext();
			}

			this._autoCloseTimer.Disposable = timerObservable.Subscribe(_ => {
				this.IsOpen.Value = false;
			});
		}
	}

	/// <summary>
	/// InfoBar が閉じられたときの処理。次の通知の表示を開始します。
	/// </summary>
	public void OnClosed() {
		this._autoCloseTimer.Disposable = null;
		this._isShowingNotification = false;
		this.ShowNextNotification();
	}
}