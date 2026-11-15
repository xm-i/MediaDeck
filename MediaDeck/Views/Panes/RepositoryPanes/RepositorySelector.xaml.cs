using CommunityToolkit.Mvvm.DependencyInjection;
using MediaDeck.Services;
using MediaDeck.ViewModels.Panes.RepositoryPanes;
using MediaDeck.Views.FolderManager;
using Microsoft.UI.Xaml;

namespace MediaDeck.Views.Panes.RepositoryPanes;

/// <summary>
/// リポジトリペインのセレクター。
/// </summary>
public sealed partial class RepositorySelector {
	/// <summary>
	/// <see cref="RepositorySelector"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	public RepositorySelector() {
		this.InitializeComponent();
	}

	/// <inheritdoc/>
	protected override void OnViewModelChanged(RepositorySelectorViewModel? oldViewModel, RepositorySelectorViewModel? newViewModel) {
		base.OnViewModelChanged(oldViewModel, newViewModel);
		newViewModel?.LoadCommand.Execute(Unit.Default);
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
/// RepositorySelector の基底クラス。UserControlBase を介して ViewModel プロパティを提供します。
/// </summary>
public abstract class RepositorySelectorUserControl : UserControlBase<RepositorySelectorViewModel>;