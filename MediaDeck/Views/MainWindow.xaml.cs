using MediaDeck.ViewModels;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace MediaDeck.Views;

[Inject(InjectServiceLifetime.Singleton)]
public sealed partial class MainWindow {
	private readonly MainWindowViewModel _viewModel;

	public MainWindow(MainWindowViewModel viewModel) {
		this._viewModel = viewModel;
		this.InitializeComponent();

		// TabItemsSource にVMのタブリストをバインド
		this.MainTabView.TabItemsSource = this._viewModel.Tabs;

		// SelectedTab の変更を追跡
		this._viewModel.SelectedTab.Subscribe(tab => {
			this.NavigationMenuControl.DataContext = tab?.NavigationMenuViewModel;
			if (tab != null && this.MainTabView.SelectedItem != tab) {
				this.MainTabView.SelectedItem = tab;
			}
		});
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
}