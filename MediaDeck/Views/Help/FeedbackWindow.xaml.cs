using MediaDeck.Composition.Interfaces;
using MediaDeck.Core.Stores.Config;
using MediaDeck.ViewModels.Help;
using MediaDeck.Views.Helpers;
using MediaDeck.Views.Resources.Converters;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace MediaDeck.Views.Help;

/// <summary>
/// フィードバックウィンドウ
/// </summary>
[Inject(InjectServiceLifetime.Transient)]
public sealed partial class FeedbackWindow : Window {
	private readonly CompositeDisposable _disposable = new();
	private readonly BinaryToImageSourceConverter _binaryToImageSourceConverter = new();

	/// <summary>
	/// <see cref="FeedbackWindow"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	/// <param name="viewModel">フィードバックウィンドウのViewModel</param>
	/// <param name="configStore">設定ストア</param>
	/// <param name="stringProvider">文字列プロバイダー</param>
	public FeedbackWindow(FeedbackWindowViewModel viewModel, IConfigStore configStore, IStringProvider stringProvider) {
		this.ViewModel = viewModel.AddTo(this._disposable);
		this.InitializeComponent();

		// ウィンドウタイトルのローカライズ設定 (Windowクラスはx:UidによるTitle設定をサポートしていないため)
		this.Title = stringProvider.GetString("FeedbackWindow_Title");

		// テーマのバインド
		ThemeHelper.BindTheme(this, configStore, this._disposable);

		this.AppWindow.Resize(new(640, 760));

		this.ViewModel.RequestClose.Subscribe(_ => {
			this.Close();
		}).AddTo(this._disposable);

		this.Closed += (s, e) => {
			this._disposable.Dispose();
		};
	}

	/// <summary>
	/// フィードバックウィンドウのViewModel
	/// </summary>
	public FeedbackWindowViewModel ViewModel {
		get;
	}

	/// <summary>
	/// バイト配列のスクリーンショットデータを <see cref="ImageSource"/> に変換します。
	/// </summary>
	/// <param name="bytes">画像データのバイト配列</param>
	/// <returns>変換された画像ソース</returns>
	public ImageSource? ConvertScreenshot(byte[]? bytes) {
		return this._binaryToImageSourceConverter.Convert(bytes!, typeof(ImageSource), null!, string.Empty) as ImageSource;
	}

	private void Window_Loaded(object sender, RoutedEventArgs e) {
		this.ExtendsContentIntoTitleBar = true;
		this.SetTitleBar(this.AppTitleBar);
	}
}