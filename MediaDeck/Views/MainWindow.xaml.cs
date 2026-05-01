using MediaDeck.ViewModels;
using MediaDeck.Views.Dialogs;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace MediaDeck.Views;

[Inject(InjectServiceLifetime.Transient)]
public sealed partial class MainWindow {
	private readonly MainWindowViewModel _viewModel;

	private readonly System.Reactive.Disposables.CompositeDisposable _disposable = new();

	public MainWindow(MainWindowViewModel viewModel) {
		this._viewModel = viewModel;
		this.InitializeComponent();

		this._viewModel.SelectedTab.Subscribe(tab => {
			if (tab != null && this.MainTabView.SelectedItem != tab) {
				this.MainTabView.SelectedItem = tab;
			}
		}).AddTo(this._disposable);

		this.Closed += (s, e) => this._disposable.Dispose();
	}

	private void MainTabView_TabCloseRequested(TabView sender, TabViewTabCloseRequestedEventArgs args) {
		if (args.Item is TabContext tabContext) {
			this._viewModel.CloseTab(tabContext);
		}
	}

	private void MainTabView_SelectionChanged(object sender, SelectionChangedEventArgs e) {
		if (this.MainTabView.SelectedItem is TabContext tabContext) {
			if (this._viewModel.SelectedTab.Value != tabContext) {
				this._viewModel.SelectedTab.Value = tabContext;
			}
		}
	}

	private void Window_Loaded(object sender, RoutedEventArgs e) {
		this._viewModel.WindowActivatedCommand.Execute(Unit.Default);
	}

	private async void TabHeader_DoubleTapped(object sender, Microsoft.UI.Xaml.Input.DoubleTappedRoutedEventArgs e) {
		if (sender is FrameworkElement fe && fe.DataContext is TabContext tabContext) {
			await this.ShowRenameTabDialogAsync(tabContext);
			e.Handled = true;
		}
	}

	private async void TabViewItem_KeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e) {
		if (e.Key == Windows.System.VirtualKey.F2) {
			if (sender is FrameworkElement fe && fe.DataContext is TabContext tabContext) {
				await this.ShowRenameTabDialogAsync(tabContext);
				e.Handled = true;
			}
		}
	}

	private async System.Threading.Tasks.Task ShowRenameTabDialogAsync(TabContext tabContext) {
		var dialog = new TabRenameDialog(tabContext.TabState.DisplayName.Value) {
			XamlRoot = this.Content.XamlRoot
		};

		var result = await dialog.ShowAsync();
		if (result == ContentDialogResult.Primary && !string.IsNullOrWhiteSpace(dialog.ResultText)) {
			tabContext.TabState.DisplayName.Value = dialog.ResultText;
		}
	}
}