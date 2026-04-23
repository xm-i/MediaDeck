using MediaDeck.ViewModels.Panes.RepositoryPanes;

namespace MediaDeck.Views.Panes.RepositoryPanes;

public sealed partial class RepositorySelector {
	public RepositorySelector() {
		this.InitializeComponent();
		this.Loaded += (_, _) => {
			this.Bindings.Update();
		};
	}
}

public abstract class RepositorySelectorUserControl : UserControlBase<RepositorySelectorViewModel>;