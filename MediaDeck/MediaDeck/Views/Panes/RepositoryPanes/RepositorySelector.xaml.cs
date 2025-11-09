using MediaDeck.Composition.Bases;
using MediaDeck.ViewModels.Panes.RepositoryPanes;

namespace MediaDeck.Views.Panes.RepositoryPanes;
public sealed partial class RepositorySelector : RepositorySelectorUserControl {
	public RepositorySelector() {
		this.InitializeComponent();
	}
}

public abstract class RepositorySelectorUserControl : UserControlBase<RepositorySelectorViewModel>;