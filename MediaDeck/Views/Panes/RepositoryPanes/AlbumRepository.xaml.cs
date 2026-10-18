using MediaDeck.ViewModels.Panes.RepositoryPanes;
using Microsoft.UI.Xaml.Input;

namespace MediaDeck.Views.Panes.RepositoryPanes;

/// <summary>
/// アルバムリポジトリペインのビュー。
/// </summary>
public sealed partial class AlbumRepository {
	/// <summary>
	/// <see cref="AlbumRepository"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	public AlbumRepository() {
		this.InitializeComponent();
	}

	private void TreeViewItem_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e) {
		if (this.ViewModel is not { } vm) {
			return;
		}
		vm.AlbumRepositoryViewModel.SetRepositoryConditionCommand.Execute(Unit.Default);
	}
}

/// <summary>
/// AlbumRepository の基底クラス。UserControlBase を介して ViewModel プロパティを提供します。
/// </summary>
public abstract class AlbumRepositoryUserControl : UserControlBase<RepositorySelectorViewModel>;