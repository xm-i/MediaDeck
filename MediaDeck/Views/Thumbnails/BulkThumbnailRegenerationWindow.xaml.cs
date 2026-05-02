using MediaDeck.Core.Stores.State;
using MediaDeck.ViewModels.Thumbnails;
using MediaDeck.Views.Helpers;

namespace MediaDeck.Views.Thumbnails;

[Inject(InjectServiceLifetime.Transient)]
public sealed partial class BulkThumbnailRegenerationWindow : Microsoft.UI.Xaml.Window {
	private readonly CompositeDisposable _disposable = new();

	public BulkThumbnailRegenerationWindow(BulkThumbnailRegenerationViewModel viewModel, IStateStore stateStore) {
		this.InitializeComponent();
		this.ViewModel = viewModel;

		// テーマのバインド
		ThemeHelper.BindTheme(this, stateStore, this._disposable);

		this.AppWindow.Resize(new(1100, 700));

		this.Closed += (s, e) => this._disposable.Dispose();
	}

	public BulkThumbnailRegenerationViewModel ViewModel {
		get;
	}

	private void Window_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e) {
		this.ExtendsContentIntoTitleBar = true;
		this.SetTitleBar(this.AppTitleBar);
	}
}