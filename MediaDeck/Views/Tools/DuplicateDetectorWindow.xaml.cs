using MediaDeck.Core.Stores.State;
using MediaDeck.ViewModels.Tools;
using MediaDeck.Views.Helpers;

namespace MediaDeck.Views.Tools;

[Inject(InjectServiceLifetime.Transient)]
public sealed partial class DuplicateDetectorWindow : Microsoft.UI.Xaml.Window {
	private readonly CompositeDisposable _disposable = new();

	public DuplicateDetectorWindow(DuplicateDetectorViewModel duplicateDetectorViewModel, IStateStore stateStore) {
		this.InitializeComponent();
		this.ViewModel = duplicateDetectorViewModel;

		// テーマのバインド
		ThemeHelper.BindTheme(this, stateStore, this._disposable);

		this.AppWindow.Resize(new(900, 600));

		this.Closed += (s, e) => this._disposable.Dispose();
	}

	public DuplicateDetectorViewModel ViewModel {
		get;
	}

	private void Window_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e) {
		this.ExtendsContentIntoTitleBar = true;
		this.SetTitleBar(this.AppTitleBar);
	}
}