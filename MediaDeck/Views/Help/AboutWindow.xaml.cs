using MediaDeck.Composition.Interfaces;
using MediaDeck.Core.Stores.Config;
using MediaDeck.Services;
using MediaDeck.ViewModels.Help;
using MediaDeck.Views.Helpers;
using Microsoft.UI.Xaml;

namespace MediaDeck.Views.Help;

/// <summary>
/// Aboutウィンドウ
/// </summary>
[Inject(InjectServiceLifetime.Transient)]
public sealed partial class AboutWindow : Window {
	private readonly CompositeDisposable _disposable = new();
	private readonly IWindowService _windowService;

	/// <summary>
	/// コンストラクタ
	/// </summary>
	/// <param name="viewModel">ViewModel</param>
	/// <param name="configStore">設定ストア</param>
	/// <param name="stringProvider">文字列プロバイダー</param>
	/// <param name="windowService">ウィンドウサービス</param>
	public AboutWindow(
		AboutWindowViewModel viewModel,
		IConfigStore configStore,
		IStringProvider stringProvider,
		IWindowService windowService) {
		this._windowService = windowService;
		this.ViewModel = viewModel.AddTo(this._disposable);
		this.InitializeComponent();

		// ウィンドウタイトルのローカライズ設定 (Windowクラスはx:UidによるTitle設定をサポートしていないため)
		this.Title = stringProvider.GetString("AboutWindow_Title");

		// テーマのバインド
		ThemeHelper.BindTheme(this, configStore, this._disposable);

		this.AppWindow.Resize(new(500, 740));

		this.Closed += (s, e) => this._disposable.Dispose();
	}

	/// <summary>
	/// ViewModel
	/// </summary>
	public AboutWindowViewModel ViewModel {
		get;
	}

	private void Window_Loaded(object sender, RoutedEventArgs e) {
		this.ExtendsContentIntoTitleBar = true;
		this.SetTitleBar(this.AppTitleBar);
	}

	/// <summary>
	/// 更新詳細ウィンドウを開くボタンのクリックイベントハンドラ
	/// </summary>
	/// <param name="sender">イベント送信元</param>
	/// <param name="e">イベント引数</param>
	private void OpenUpdateNotificationButton_Click(object sender, RoutedEventArgs e) {
		this._windowService.ActivateCentered<UpdateNotificationWindow>(this);
	}
}