using Microsoft.UI.Xaml;

namespace MediaDeck.Views.Panes.ViewerPanes;
public sealed partial class MapViewer : ViewerPaneBase {
	public MapViewer() {
		this.InitializeComponent();
	}

	private void Map_Loaded(object sender, RoutedEventArgs e) {
		if(this.ViewModel is not { } vm) {
			return;
		}
		vm.MapViewerViewModel.UpdateMapControl(this.Map);
	}
}