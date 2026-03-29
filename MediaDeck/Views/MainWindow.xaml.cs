using Microsoft.UI.Xaml;

using MediaDeck.ViewModels;

namespace MediaDeck.Views;

[Inject(InjectServiceLifetime.Singleton)]
public sealed partial class MainWindow {
	private readonly MainWindowViewModel _viewModel;

	public MainWindow(MainWindowViewModel viewModel) {
		this._viewModel = viewModel;
		this.InitializeComponent();
	}

	private void Window_Loaded(object sender, RoutedEventArgs e) {
		this._viewModel.WindowActivatedCommand.Execute(Unit.Default);
	}
}