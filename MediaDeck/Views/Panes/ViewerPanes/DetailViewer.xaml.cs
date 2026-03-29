using CommunityToolkit.WinUI;

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace MediaDeck.Views.Panes.ViewerPanes;

public sealed partial class DetailViewer : ViewerPaneBase {
	public DetailViewer() {
		this.InitializeComponent();
		this.Loaded += (s, e) => {
			this.List.AddHandler(PointerWheelChangedEvent, new PointerEventHandler(this.HandleListPointerWheelChanged), true);
		};
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