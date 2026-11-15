using MediaDeck.Composition.Interfaces;
using MediaDeck.Core.Stores.Config;
using MediaDeck.ViewModels.Help;
using MediaDeck.Views.Helpers;
using Microsoft.UI.Xaml;

namespace MediaDeck.Views.Help;

/// <summary>
/// 新バージョンの更新通知ウィンドウ
/// </summary>
[Inject(InjectServiceLifetime.Transient)]
public sealed partial class UpdateNotificationWindow : Window {
	private readonly CompositeDisposable _disposable = new();

	/// <summary>
	/// <see cref="UpdateNotificationWindow"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	/// <param name="viewModel">更新通知ウィンドウのViewModel</param>
	/// <param name="configStore">設定ストア</param>
	/// <param name="stringProvider">文字列プロバイダー</param>
	public UpdateNotificationWindow(
		UpdateNotificationWindowViewModel viewModel,
		IConfigStore configStore,
		IStringProvider stringProvider) {
		this.ViewModel = viewModel.AddTo(this._disposable);
		this.InitializeComponent();

		// ウィンドウタイトルのローカライズ設定 (Windowクラスはx:UidによるTitle設定をサポートしていないため)
		this.Title = stringProvider.GetString("UpdateNotificationWindow_Title");

		// テーマのバインド
		ThemeHelper.BindTheme(this, configStore, this._disposable);

		this.AppWindow.Resize(new(540, 560));

		this.ViewModel.RequestClose.Subscribe(_ => {
			this.Close();
		}).AddTo(this._disposable);

		this.Closed += (s, e) => {
			this._disposable.Dispose();
		};
	}

	/// <summary>
	/// 更新通知ウィンドウのViewModel
	/// </summary>
	public UpdateNotificationWindowViewModel ViewModel {
		get;
	}

	private void Window_Loaded(object sender, RoutedEventArgs e) {
		this.ExtendsContentIntoTitleBar = true;
		this.SetTitleBar(this.AppTitleBar);
	}
}