using MediaDeck.Composition.Interfaces;
using MediaDeck.Core.Stores.Config;
using MediaDeck.ViewModels.Tools;
using MediaDeck.Views.Helpers;

namespace MediaDeck.Views.Tools;

[Inject(InjectServiceLifetime.Transient)]
public sealed partial class DuplicateDetectorWindow : Microsoft.UI.Xaml.Window {
	private readonly CompositeDisposable _disposable = new();

	/// <summary>
	/// <see cref="DuplicateDetectorWindow"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	/// <param name="duplicateDetectorViewModel">重複検出ViewModel</param>
	/// <param name="configStore">設定ストア</param>
	/// <param name="stringProvider">文字列プロバイダー</param>
	public DuplicateDetectorWindow(DuplicateDetectorViewModel duplicateDetectorViewModel, IConfigStore configStore, IStringProvider stringProvider) {
		this.ViewModel = duplicateDetectorViewModel.AddTo(this._disposable);
		this.InitializeComponent();

		this.Title = stringProvider.GetString("DuplicateDetector_WindowTitle");

		// テーマのバインド
		ThemeHelper.BindTheme(this, configStore, this._disposable);

		this.AppWindow.Resize(new(900, 600));

		this.Closed += (s, e) => this._disposable.Dispose();
	}

	/// <summary>
	/// 重複検出ViewModel
	/// </summary>
	public DuplicateDetectorViewModel ViewModel {
		get;
	}

	private void Window_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e) {
		this.ExtendsContentIntoTitleBar = true;
		this.SetTitleBar(this.AppTitleBar);
	}
}