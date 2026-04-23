using MediaDeck.ViewModels.Panes.RepositoryPanes;

using Microsoft.UI.Xaml.Input;

namespace MediaDeck.Views.Panes.RepositoryPanes;

public sealed partial class FolderRepository {
	public FolderRepository() {
		this.InitializeComponent();
		this.Loaded += (_, _) => {
			this.Bindings.Update();
		};
	}

	protected override void OnViewModelChanged(RepositorySelectorViewModel? oldViewModel, RepositorySelectorViewModel? newViewModel) {
		base.OnViewModelChanged(oldViewModel, newViewModel);
		newViewModel?.LoadCommand.Execute(Unit.Default);
	}

	private void TreeViewItem_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e) {
		if (this.ViewModel is not { } vm) {
			return;
		}
		vm.FolderRepositoryViewModel.SetRepositoryConditionCommand.Execute(Unit.Default);
	}
}

public abstract class FolderRepositoryUserControl : UserControlBase<RepositorySelectorViewModel>;