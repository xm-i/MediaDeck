using Microsoft.UI.Xaml.Input;

namespace MediaDeck.Views.Panes.ViewerPanes;

public sealed partial class ListViewer : ViewerPaneBase {
	public ListViewer() {
		this.InitializeComponent();
		this.Loaded += (s, e) => {
			this.List.AddHandler(PointerWheelChangedEvent, new PointerEventHandler(this.HandleListPointerWheelChanged), true);
		};
	}
}