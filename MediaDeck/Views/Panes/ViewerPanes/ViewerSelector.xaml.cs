using MediaDeck.ViewModels.Panes.ViewerPanes;

namespace MediaDeck.Views.Panes.ViewerPanes;

public sealed partial class ViewerSelector {
	public ViewerSelector() {
		this.InitializeComponent();
		this.Loaded += (_, _) => {
			this.Bindings.Update();
		};
	}
}


public abstract class ViewerSelectorUserControl : UserControlBase<ViewerSelectorViewModel>;