using MediaDeck.Composition.Objects;
using MediaDeck.ViewModels.Controls;
using Microsoft.UI.Xaml.Controls;

namespace MediaDeck.Views.Controls;

/// <summary>
/// アプリケーション全体の通知をInfoBarで表示するコントロール
/// </summary>
public sealed partial class GlobalInfoBar {
	/// <summary>
	/// GlobalInfoBarクラスの新しいインスタンスを初期化
	/// </summary>
	public GlobalInfoBar() {
		this.InitializeComponent();
	}

	/// <summary>
	/// NotificationSeverity を InfoBarSeverity に変換します。
	/// </summary>
	/// <param name="severity">通知の重要度</param>
	/// <returns>InfoBar の重要度</returns>
	public InfoBarSeverity ToInfoBarSeverity(NotificationSeverity severity) {
		return severity switch {
			NotificationSeverity.Informational => InfoBarSeverity.Informational,
			NotificationSeverity.Success => InfoBarSeverity.Success,
			NotificationSeverity.Warning => InfoBarSeverity.Warning,
			NotificationSeverity.Error => InfoBarSeverity.Error,
			_ => InfoBarSeverity.Informational
		};
	}

	/// <summary>
	/// InfoBarが閉じられたときの処理。次の通知の表示を開始
	/// </summary>
	/// <param name="sender">イベント送信元</param>
	/// <param name="args">イベント引数</param>
	private void InfoBar_Closed(InfoBar sender, InfoBarClosedEventArgs args) {
		this.ViewModel?.OnClosed();
	}
}

/// <summary>
/// GlobalInfoBarの基底クラス。UserControlBaseを介してViewModelプロパティを提供します。
/// </summary>
public abstract class GlobalInfoBarUserControl : UserControlBase<GlobalInfoBarViewModel>;