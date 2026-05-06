using MediaDeck.ViewModels.Tools;
using Microsoft.UI.Xaml;

namespace MediaDeck.Views.Panes.StatusPanes;

/// <summary>
/// バックグラウンドタスクの状態をステータスバーに表示するユーザーコントロール。
/// </summary>
public sealed partial class BackgroundTasksStatusIndicator {
	public BackgroundTasksStatusIndicator() {
		this.InitializeComponent();
	}

	/// <summary>
	/// bool値を表示状態に変換する。
	/// </summary>
	/// <param name="value">表示するかどうか</param>
	/// <returns>変換後の表示状態</returns>
	private Visibility ToVisibility(bool value) {
		return value ? Visibility.Visible : Visibility.Collapsed;
	}

	/// <summary>
	/// bool値を反転した表示状態に変換する。
	/// </summary>
	/// <param name="value">非表示にするかどうか</param>
	/// <returns>変換後の表示状態</returns>
	private Visibility ToInverseVisibility(bool value) {
		return value ? Visibility.Collapsed : Visibility.Visible;
	}
}

/// <summary>
/// BackgroundTasksStatusIndicatorの基底クラス。
/// </summary>
public abstract class BackgroundTasksStatusIndicatorUserControl : UserControlBase<BackgroundTasksViewModel>;