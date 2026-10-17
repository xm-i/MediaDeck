using MediaDeck.Core.Stores.Config;
using MediaDeck.ViewModels.Thumbnails;
using MediaDeck.Views.Helpers;

namespace MediaDeck.Views.Thumbnails;

[Inject(InjectServiceLifetime.Transient)]
public sealed partial class BulkThumbnailRegenerationWindow : Microsoft.UI.Xaml.Window {
	private readonly CompositeDisposable _disposable = new();

	/// <summary>
	/// <see cref="BulkThumbnailRegenerationWindow"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	/// <param name="viewModel">一括サムネイル再生成ViewModel</param>
	/// <param name="configStore">設定ストア</param>
	public BulkThumbnailRegenerationWindow(BulkThumbnailRegenerationViewModel viewModel, IConfigStore configStore) {
		this.InitializeComponent();
		this.ViewModel = viewModel.AddTo(this._disposable);

		// テーマのバインド
		ThemeHelper.BindTheme(this, configStore, this._disposable);

		this.AppWindow.Resize(new(1100, 700));

		this.Closed += (s, e) => this._disposable.Dispose();
	}

	/// <summary>
	/// 一括サムネイル再生成ViewModel
	/// </summary>
	public BulkThumbnailRegenerationViewModel ViewModel {
		get;
	}

	private void Window_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e) {
		this.ExtendsContentIntoTitleBar = true;
		this.SetTitleBar(this.AppTitleBar);
	}
}