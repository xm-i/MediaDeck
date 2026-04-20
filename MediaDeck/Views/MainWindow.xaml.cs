using MediaDeck.ViewModels;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace MediaDeck.Views;

[Inject(InjectServiceLifetime.Singleton)]
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
}