using System.Threading.Tasks;
using CommunityToolkit.Mvvm.DependencyInjection;
using MediaDeck.Core.Stores.State;
using MediaDeck.ViewModels;
using MediaDeck.Views.Dialogs;
using MediaDeck.Views.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace MediaDeck.Views;

[Inject(InjectServiceLifetime.Transient)]
public sealed partial class MainWindow : Window {
	private readonly MainWindowViewModel _viewModel;
	private readonly CompositeDisposable _disposable = new();

	public MainWindow(MainWindowViewModel viewModel, IStateStore stateStore) {
		this._viewModel = viewModel;
		this.InitializeComponent();

		// テーマのバインド
		ThemeHelper.BindTheme(this, stateStore, this._disposable);

		// カスタムタイトルバーの設定
		this.ExtendsContentIntoTitleBar = true;
		this.SetTitleBar(this.AppTitleBar);

		this._viewModel.SelectedTab.Subscribe(tab => {
			if (tab != null && this.MainTabView.SelectedItem != tab) {
				this.MainTabView.SelectedItem = tab;
			}
		}).AddTo(this._disposable);

		this.Closed += (s, e) => this._disposable.Dispose();

		this._viewModel.WindowActivatedCommand.Execute(Unit.Default);
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

	private async Task ShowRenameTabDialogAsync(TabContext tabContext) {
		var dialog = Ioc.Default.GetRequiredService<TabRenameDialog>();
		dialog.XamlRoot = this.Content.XamlRoot;
		dialog.Initialize(tabContext.TabState.DisplayName.Value);

		var result = await dialog.ShowAsync();
		if (result == ContentDialogResult.Primary && !string.IsNullOrWhiteSpace(dialog.ResultText)) {
			tabContext.TabState.DisplayName.Value = dialog.ResultText;
		}
	}
}