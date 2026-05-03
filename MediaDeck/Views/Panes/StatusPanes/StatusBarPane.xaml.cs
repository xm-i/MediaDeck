using MediaDeck.ViewModels;
using MediaDeck.ViewModels.Panes.ViewerPanes;

namespace MediaDeck.Views.Panes.StatusPanes;

/// <summary>
/// ステータスバーを表示するユーザーコントロール
/// </summary>
public sealed partial class StatusBarPane {
	public StatusBarPane() {
		this.InitializeComponent();
	}

	private void ViewerModeSegmented_SelectionChanged(object sender, Microsoft.UI.Xaml.Controls.SelectionChangedEventArgs e) {
		if (this.ViewModel == null) {
			return;
		}

		if (e.AddedItems.Count != 1 || e.AddedItems[0] is not ViewerPaneViewModelBase selectedViewerPane) {
			return;
		}

		this.ViewModel.ViewerSelector.SelectedViewerPane.Value = selectedViewerPane;
	}
}

/// <summary>
/// StatusBarPaneの基底クラス。UserControlBaseを介してViewModelプロパティを提供します。
/// </summary>
public abstract class StatusBarPaneUserControl : UserControlBase<StatusBarViewModel>;