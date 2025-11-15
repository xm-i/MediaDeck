using Microsoft.UI.Xaml;

using MediaDeck.ViewModels.FolderManager;

using Windows.Graphics;

namespace MediaDeck.Views.FolderManager;

[AddTransient]
public sealed partial class FolderManagerWindow : Window {
	public FolderManagerViewModel ViewModel {
		get;
	}
	public FolderManagerWindow(FolderManagerViewModel viewModel) {
		this.InitializeComponent();
		this.ViewModel = viewModel;
		this.AppWindow.Resize(new SizeInt32(1000, 700));
	}
}
