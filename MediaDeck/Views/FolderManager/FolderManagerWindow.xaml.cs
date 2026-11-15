using MediaDeck.Composition.Interfaces;
using MediaDeck.Core.Stores.Config;
using MediaDeck.ViewModels.FolderManager;
using MediaDeck.Views.Helpers;

namespace MediaDeck.Views.FolderManager;

[Inject(InjectServiceLifetime.Transient)]
public sealed partial class FolderManagerWindow : Microsoft.UI.Xaml.Window {
	private readonly CompositeDisposable _disposable = new();

	/// <summary>
	/// <see cref="FolderManagerWindow"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	/// <param name="viewModel">フォルダ管理ViewModel</param>
	/// <param name="configStore">設定ストア</param>
	/// <param name="stringProvider">文字列プロバイダー</param>
	public FolderManagerWindow(FolderManagerViewModel viewModel, IConfigStore configStore, IStringProvider stringProvider) {
		this.ViewModel = viewModel.AddTo(this._disposable);
		this.InitializeComponent();

		this.Title = stringProvider.GetString("FolderManager_WindowTitle");

		// テーマのバインド
		ThemeHelper.BindTheme(this, configStore, this._disposable);

		this.AppWindow.Resize(new(1000, 700));

		this.Closed += (s, e) => this._disposable.Dispose();
	}

	/// <summary>
	/// フォルダ管理ViewModel
	/// </summary>
	public FolderManagerViewModel ViewModel {
		get;
	}

	private void Window_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e) {
		this.ExtendsContentIntoTitleBar = true;
		this.SetTitleBar(this.AppTitleBar);
	}
}