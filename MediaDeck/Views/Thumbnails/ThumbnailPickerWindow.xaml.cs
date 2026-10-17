using MediaDeck.Core.Stores.Config;
using MediaDeck.ViewModels.Thumbnails;
using MediaDeck.Views.Helpers;

namespace MediaDeck.Views.Thumbnails;

[Inject(InjectServiceLifetime.Transient)]
public sealed partial class ThumbnailPickerWindow : Microsoft.UI.Xaml.Window {
	private readonly CompositeDisposable _disposable = new();

	/// <summary>
	/// <see cref="ThumbnailPickerWindow"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	/// <param name="thumbnailPickerSelectorViewModel">サムネイルピッカーセレクターViewModel</param>
	/// <param name="configStore">設定ストア</param>
	public ThumbnailPickerWindow(ThumbnailPickerSelectorViewModel thumbnailPickerSelectorViewModel, IConfigStore configStore) {
		this.InitializeComponent();
		this.ViewModel = thumbnailPickerSelectorViewModel.AddTo(this._disposable);

		// テーマのバインド
		ThemeHelper.BindTheme(this, configStore, this._disposable);

		this.AppWindow.Resize(new(1000, 700));

		this.Closed += (s, e) => this._disposable.Dispose();
	}

	/// <summary>
	/// サムネイルピッカーセレクターViewModel
	/// </summary>
	public ThumbnailPickerSelectorViewModel ViewModel {
		get;
	}

	private void Window_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e) {
		this.ExtendsContentIntoTitleBar = true;
		this.SetTitleBar(this.AppTitleBar);
	}
}