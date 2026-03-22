using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using MediaDeck.Models.Services;
using MediaDeck.ViewModels;

using Windows.Graphics;

namespace MediaDeck.Views;

[Inject(InjectServiceLifetime.Transient)]
public sealed partial class FileChangeSyncWindow : Window {
	public FileChangeSyncWindow(FileChangeSyncViewModel viewModel) {
		this.ViewModel = viewModel;
		this.InitializeComponent();
		this.AppWindow.Resize(new SizeInt32(700, 500));
	}

	public FileChangeSyncViewModel ViewModel {
		get;
	}

	private void ApplyItem_Click(object sender, RoutedEventArgs e) {
		if (sender is Button { Tag: FileChangeItem item }) {
			this.ViewModel.ApplySingleCommand.Execute(item);
		}
	}

	private void DiscardItem_Click(object sender, RoutedEventArgs e) {
		if (sender is Button { Tag: FileChangeItem item }) {
			this.ViewModel.DiscardSingleCommand.Execute(item);
		}
	}
}
