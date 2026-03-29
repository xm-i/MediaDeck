using Microsoft.UI.Xaml.Input;

namespace MediaDeck.Views.Panes.ViewerPanes;

public sealed partial class WrapViewer : ViewerPaneBase {
	public WrapViewer() {
		this.InitializeComponent();
		this.Loaded += (s, e) => {
			this.List.AddHandler(PointerWheelChangedEvent, new PointerEventHandler(this.HandleListPointerWheelChanged), true);
		};
	}
}