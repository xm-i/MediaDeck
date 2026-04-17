using MediaDeck.Core.Services.FileChangeMonitor;
using MediaDeck.ViewModels;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace MediaDeck.Views;

[Inject(InjectServiceLifetime.Transient)]
public sealed partial class FileChangeSyncWindow {
	public FileChangeSyncWindow(FileChangeSyncViewModel viewModel) {
		this.ViewModel = viewModel;
		this.InitializeComponent();
		this.AppWindow.Resize(new(700, 500));
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