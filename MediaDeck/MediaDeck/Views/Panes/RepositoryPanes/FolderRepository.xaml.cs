using MediaDeck.Composition.Bases;
using MediaDeck.ViewModels.Panes.RepositoryPanes;

namespace MediaDeck.Views.Panes.RepositoryPanes;
public sealed partial class FolderRepository : FolderRepositoryUserControl {
	public FolderRepository() {
		this.InitializeComponent();
	}

	protected override void OnViewModelChanged(RepositorySelectorViewModel? oldViewModel, RepositorySelectorViewModel? newViewModel) {
		base.OnViewModelChanged(oldViewModel, newViewModel);
		if (newViewModel == null) {
			return;
		}
		newViewModel.LoadCommand.Execute(Unit.Default);
	}

	private void TreeViewItem_DoubleTapped(object sender, Microsoft.UI.Xaml.Input.DoubleTappedRoutedEventArgs e) {
		if (this.ViewModel is not { } vm) {
			return;
		}
		vm.FolderRepositoryViewModel.SetRepositoryConditionCommand.Execute(Unit.Default);
    }
}

public abstract class FolderRepositoryUserControl : UserControlBase<RepositorySelectorViewModel>;