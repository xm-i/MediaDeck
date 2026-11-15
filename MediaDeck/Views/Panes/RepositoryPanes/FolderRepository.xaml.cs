using CommunityToolkit.Mvvm.DependencyInjection;
using MediaDeck.Services;
using MediaDeck.ViewModels.Panes.RepositoryPanes;
using MediaDeck.Views.FolderManager;
using Microsoft.UI.Xaml;
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

	private void TreeViewItem_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e) {
		if (this.ViewModel is not { } vm) {
			return;
		}
		vm.SetRepositoryConditionCommand.Execute(Unit.Default);
	}

	/// <summary>
	/// フォルダ管理ウィンドウを開きます。
	/// </summary>
	/// <param name="sender">イベント発生元。</param>
	/// <param name="e">イベント引数。</param>
	private void OpenFolderManagerButton_Click(object sender, RoutedEventArgs e) {
		Ioc.Default.GetRequiredService<IWindowService>().ActivateCentered<FolderManagerWindow>(this);
	}
}

/// <summary>
/// FolderRepository の基底クラス。UserControlBase を介して ViewModel プロパティを提供します。
/// </summary>
public abstract class FolderRepositoryUserControl : UserControlBase<FolderRepositoryViewModel>;