using MediaDeck.Core.Services.FileChangeMonitor;
using MediaDeck.Core.Stores.State;
using MediaDeck.ViewModels;
using MediaDeck.Views.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace MediaDeck.Views;

[Inject(InjectServiceLifetime.Transient)]
public sealed partial class FileChangeSyncWindow : Window {
	private readonly CompositeDisposable _disposable = new();

	public FileChangeSyncWindow(FileChangeSyncViewModel viewModel, IStateStore stateStore) {
		this.ViewModel = viewModel;
		this.InitializeComponent();

		// テーマのバインド
		ThemeHelper.BindTheme(this, stateStore, this._disposable);

		this.AppWindow.Resize(new(700, 500));

		this.Closed += (s, e) => this._disposable.Dispose();
	}

	public FileChangeSyncViewModel ViewModel {
		get;
	}

	private void Window_Loaded(object sender, RoutedEventArgs e) {
		this.ExtendsContentIntoTitleBar = true;
		this.SetTitleBar(this.AppTitleBar);
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