using MediaDeck.Composition.Interfaces;
using MediaDeck.Core.Stores.Config;
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

	/// <summary>
	/// コンストラクタ
	/// </summary>
	/// <param name="viewModel">ViewModel</param>
	/// <param name="configStore">設定ストア</param>
	/// <param name="stringProvider">文字列プロバイダー</param>
	public AboutWindow(AboutWindowViewModel viewModel, IConfigStore configStore, IStringProvider stringProvider) {
		this.ViewModel = viewModel.AddTo(this._disposable);
		this.InitializeComponent();

		// ウィンドウタイトルのローカライズ設定 (Windowクラスはx:UidによるTitle設定をサポートしていないため)
		this.Title = stringProvider.GetString("AboutWindow_Title");

		// テーマのバインド
		ThemeHelper.BindTheme(this, configStore, this._disposable);

		this.AppWindow.Resize(new(500, 650));

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
}