using MediaDeck.Core.Stores.State;
using MediaDeck.ViewModels.Sort;
using MediaDeck.Views.Helpers;

namespace MediaDeck.Views.Sort;

[Inject(InjectServiceLifetime.Transient)]
public sealed partial class SortManagerWindow : Microsoft.UI.Xaml.Window {
	private readonly CompositeDisposable _disposable = new();

	public SortManagerWindow(SortManagerViewModel viewModel, IStateStore stateStore) {
		this.ViewModel = viewModel;
		this.InitializeComponent();

		// テーマのバインド
		ThemeHelper.BindTheme(this, stateStore, this._disposable);

		this.AppWindow.Resize(new(1000, 700));

		this.Closed += (s, e) => this._disposable.Dispose();
	}

	public SortManagerViewModel ViewModel {
		get;
	}

	private void Window_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e) {
		this.ExtendsContentIntoTitleBar = true;
		this.SetTitleBar(this.AppTitleBar);
	}
}