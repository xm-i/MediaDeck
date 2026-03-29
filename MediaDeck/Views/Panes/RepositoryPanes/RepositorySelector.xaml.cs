using MediaDeck.ViewModels.Panes.RepositoryPanes;

namespace MediaDeck.Views.Panes.RepositoryPanes;

public sealed partial class RepositorySelector {
	public RepositorySelector() {
		this.InitializeComponent();
	}
}

public abstract class RepositorySelectorUserControl : UserControlBase<RepositorySelectorViewModel>;