using MediaDeck.Composition.Interfaces;
using MediaDeck.Core.Stores.Config;
using MediaDeck.ViewModels.Sort;
using MediaDeck.Views.Helpers;

namespace MediaDeck.Views.Sort;

[Inject(InjectServiceLifetime.Transient)]
public sealed partial class SortManagerWindow : Microsoft.UI.Xaml.Window {
	private readonly CompositeDisposable _disposable = new();

	/// <summary>
	/// <see cref="SortManagerWindow"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	/// <param name="viewModel">ソート管理ViewModel</param>
	/// <param name="configStore">設定ストア</param>
	/// <param name="stringProvider">文字列プロバイダー</param>
	public SortManagerWindow(SortManagerViewModel viewModel, IConfigStore configStore, IStringProvider stringProvider) {
		this.ViewModel = viewModel.AddTo(this._disposable);
		this.InitializeComponent();

		this.Title = stringProvider.GetString("SortManager_WindowTitle");

		// テーマのバインド
		ThemeHelper.BindTheme(this, configStore, this._disposable);

		this.AppWindow.Resize(new(1000, 700));
		this.ViewModel.RequestClose.Subscribe(_ => {
			this.Close();
		}).AddTo(this._disposable);

		this.Closed += (s, e) => this._disposable.Dispose();
	}

	/// <summary>
	/// ソート管理ViewModel
	/// </summary>
	public SortManagerViewModel ViewModel {
		get;
	}

	private void Window_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e) {
		this.ExtendsContentIntoTitleBar = true;
		this.SetTitleBar(this.AppTitleBar);
	}
}