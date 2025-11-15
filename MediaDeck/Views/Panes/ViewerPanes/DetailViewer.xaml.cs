using CommunityToolkit.WinUI;

using Microsoft.UI.Xaml.Controls;

namespace MediaDeck.Views.Panes.ViewerPanes;
public sealed partial class DetailViewer : ViewerPaneBase {
	public DetailViewer() {
		this.InitializeComponent();
	}
	protected override async void List_SelectionChanged(object sender, SelectionChangedEventArgs e) {
		if (sender is ListView listView) {
			if (listView.SelectedIndex == -1) {
				return;
			}
			await listView.SmoothScrollIntoViewWithIndexAsync(listView.SelectedIndex, ScrollItemPlacement.Center, false, true);
		}
	}
}

