using MediaDeck.ViewModels.Panes.RepositoryPanes;

using Microsoft.UI.Xaml.Input;

namespace MediaDeck.Views.Panes.RepositoryPanes;

/// <summary>
/// フォルダリポジトリペインのビュー。
/// </summary>
public sealed partial class FolderRepository {
	/// <summary>
	/// <see cref="FolderRepository"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	public FolderRepository() {
		this.InitializeComponent();
	}

	/// <inheritdoc/>
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

/// <summary>
/// FolderRepository の基底クラス。UserControlBase を介して ViewModel プロパティを提供します。
/// </summary>
public abstract class FolderRepositoryUserControl : UserControlBase<RepositorySelectorViewModel>;