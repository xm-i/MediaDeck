using Microsoft.UI.Xaml.Input;

namespace MediaDeck.Views.Panes.ViewerPanes;

public sealed partial class ListViewer {
	public ListViewer() {
		this.InitializeComponent();
		this.Loaded += (_, _) => {
			this.List.AddHandler(PointerWheelChangedEvent, new PointerEventHandler(this.HandleListPointerWheelChanged), true);
		};
	}
}