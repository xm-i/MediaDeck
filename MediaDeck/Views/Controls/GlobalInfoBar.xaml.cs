using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

using MediaDeck.Composition.Objects;
using MediaDeck.Core.Models.NotificationDispatcher;

using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace MediaDeck.Views.Controls;

/// <summary>
/// アプリケーション全体の通知をInfoBarで表示するコントロール
/// </summary>
public sealed partial class GlobalInfoBar : UserControl {
	private readonly AppNotificationDispatcher _dispatcher;
	private readonly ConcurrentQueue<InfoBarNotification> _notificationQueue = new();
	private readonly DispatcherQueue _dispatcherQueue;
	private CancellationTokenSource? _autoCloseCts;
	private bool _isShowingNotification;

	/// <summary>
	/// GlobalInfoBarクラスの新しいインスタンスを初期化
	/// </summary>
	public GlobalInfoBar() {
		this.InitializeComponent();
		this._dispatcherQueue = DispatcherQueue.GetForCurrentThread();
		this._dispatcher = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetRequiredService<AppNotificationDispatcher>();
		this._dispatcher.Notify
			.ObserveOnCurrentSynchronizationContext()
			.Subscribe(appNotification => {
				var infoBarNotification = InfoBarNotification.FromAppNotification(appNotification);
				this.EnqueueNotification(infoBarNotification);
			});
	}

	/// <summary>
	/// 通知をキューに追加し、表示処理を開始
	/// </summary>
	/// <param name="notification">表示する通知</param>
	private void EnqueueNotification(InfoBarNotification notification) {
		this._notificationQueue.Enqueue(notification);
		this.ShowNextNotification();
	}

	/// <summary>
	/// キューから次の通知を取り出して表示
	/// </summary>
	private void ShowNextNotification() {
		if (this._isShowingNotification) {
			return;
		}

		if (!this._notificationQueue.TryDequeue(out var notification)) {
			return;
		}

		this._isShowingNotification = true;
		this._autoCloseCts?.Cancel();
		this._autoCloseCts = new CancellationTokenSource();

		this.NotificationInfoBar.Title = notification.Title;
		this.NotificationInfoBar.Message = notification.Message;
		this.NotificationInfoBar.Severity = notification.Severity switch {
			NotificationSeverity.Informational => InfoBarSeverity.Informational,
			NotificationSeverity.Success => InfoBarSeverity.Success,
			NotificationSeverity.Warning => InfoBarSeverity.Warning,
			NotificationSeverity.Error => InfoBarSeverity.Error,
			_ => InfoBarSeverity.Informational
		};
		this.NotificationInfoBar.IsOpen = true;

		if (notification.AutoCloseMilliseconds > 0) {
			this.StartAutoCloseTimer(notification.AutoCloseMilliseconds, this._autoCloseCts.Token);
		}
	}

	/// <summary>
	/// 指定時間後にInfoBarを自動的に閉じるタイマーを開始
	/// </summary>
	/// <param name="milliseconds">待機時間（ミリ秒）</param>
	/// <param name="ct">キャンセルトークン</param>
	private async void StartAutoCloseTimer(int milliseconds, CancellationToken ct) {
		try {
			await Task.Delay(milliseconds, ct);
			this._dispatcherQueue.TryEnqueue(() => {
				if (!ct.IsCancellationRequested) {
					this.NotificationInfoBar.IsOpen = false;
				}
			});
		} catch (TaskCanceledException) {
			// キャンセルされた場合は何もしない
		}
	}

	/// <summary>
	/// InfoBarが閉じられたときの処理。次の通知の表示を開始
	/// </summary>
	/// <param name="sender">イベント送信元</param>
	/// <param name="args">イベント引数</param>
	private void InfoBar_Closed(InfoBar sender, InfoBarClosedEventArgs args) {
		this._autoCloseCts?.Cancel();
		this._isShowingNotification = false;
		this.ShowNextNotification();
	}
}
