using MediaDeck.Core.Stores.State;
using MediaDeck.ViewModels.Tools;
using MediaDeck.Views.Helpers;

namespace MediaDeck.Views.Tools;

[Inject(InjectServiceLifetime.Transient)]
public sealed partial class BackgroundTasksWindow : Microsoft.UI.Xaml.Window {
	private readonly CompositeDisposable _disposable = new();

	public BackgroundTasksWindow(BackgroundTasksViewModel backgroundTasksViewModel, IStateStore stateStore) {
		this.InitializeComponent();
		this.ViewModel = backgroundTasksViewModel;

		// テーマのバインド
		ThemeHelper.BindTheme(this, stateStore, this._disposable);

		this.Closed += (s, e) => this._disposable.Dispose();
	}

	public BackgroundTasksViewModel ViewModel {
		get;
	}

	private void Window_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e) {
		this.ExtendsContentIntoTitleBar = true;
		this.SetTitleBar(this.AppTitleBar);
	}
}