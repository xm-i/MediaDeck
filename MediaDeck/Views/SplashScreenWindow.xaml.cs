using MediaDeck.Composition.Interfaces;
using MediaDeck.Core.Stores.Config;
using MediaDeck.ViewModels;
using MediaDeck.Views.Helpers;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;

namespace MediaDeck.Views;

/// <summary>
/// アプリケーション起動時に表示されるスプラッシュ画面。
/// </summary>
public sealed partial class SplashScreenWindow : Window {
	/// <summary>
	/// スプラッシュ画面のViewModel。
	/// </summary>
	public SplashScreenViewModel ViewModel {
		get;
	}

	private readonly CompositeDisposable _disposable = new();

	public SplashScreenWindow(IConfigStore configStore, IStringProvider? stringProvider = null) {
		this.ViewModel = new SplashScreenViewModel(stringProvider).AddTo(this._disposable);
		this.InitializeComponent();

		// テーマを適用
		ThemeHelper.BindTheme(this, configStore, this._disposable);

		// ウィンドウタイトルを設定
		this.Title = "MediaDeck";

		// タイトルバーを非表示にし、コンテンツを拡張する
		this.ExtendsContentIntoTitleBar = true;
		this.SetTitleBar(null);

		var appWindow = this.AppWindow;

		// ウィンドウのスタイル設定
		if (appWindow.Presenter is OverlappedPresenter presenter) {
			presenter.IsResizable = false;
			presenter.IsMaximizable = false;
			presenter.IsMinimizable = false;
		}

		// ウィンドウサイズの指定 (420pxの画像 + 余白)
		appWindow.Resize(new Windows.Graphics.SizeInt32(640, 480));

		// 画面中央に配置
		this.CenterWindow();

		this.Closed += (s, e) => this._disposable.Dispose();
	}

	/// <summary>
	/// ウィンドウをプライマリディスプレイの中央に配置します。
	/// </summary>
	private void CenterWindow() {
		var appWindow = this.AppWindow;
		var displayArea = DisplayArea.GetFromWindowId(appWindow.Id, DisplayAreaFallback.Primary);
		if (displayArea != null) {
			var centeredPosition = appWindow.Position;
			centeredPosition.X = (displayArea.WorkArea.Width - appWindow.Size.Width) / 2;
			centeredPosition.Y = (displayArea.WorkArea.Height - appWindow.Size.Height) / 2;

			// タスクバーなどを考慮したWorkAreaの開始位置を加算
			centeredPosition.X += displayArea.WorkArea.X;
			centeredPosition.Y += displayArea.WorkArea.Y;

			appWindow.Move(centeredPosition);
		}
	}
}