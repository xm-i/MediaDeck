using MediaDeck.Composition.Interfaces;
using MediaDeck.Core.Services.FileChangeMonitor;
using MediaDeck.Core.Stores.Config;
using MediaDeck.ViewModels;
using MediaDeck.Views.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace MediaDeck.Views;

/// <summary>
/// 未処理のファイル変更の同期確認ウィンドウ。
/// </summary>
[Inject(InjectServiceLifetime.Transient)]
public sealed partial class FileChangeSyncWindow : Window {
	private readonly CompositeDisposable _disposable = new();

	/// <summary>
	/// <see cref="FileChangeSyncWindow"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	/// <param name="viewModel">ファイル変更同期 ViewModel</param>
	/// <param name="configStore">設定ストア</param>
	/// <param name="stringProvider">文字列リソースプロバイダー</param>
	public FileChangeSyncWindow(FileChangeSyncViewModel viewModel, IConfigStore configStore, IStringProvider stringProvider) {
		this.ViewModel = viewModel.AddTo(this._disposable);
		this.Title = stringProvider.GetString("FileChangeSync_Title");
		this.InitializeComponent();

		// テーマのバインド
		ThemeHelper.BindTheme(this, configStore, this._disposable);

		this.AppWindow.Resize(new(700, 500));

		this.Closed += (s, e) => this._disposable.Dispose();
	}

	/// <summary>
	/// ファイル変更同期 ViewModel
	/// </summary>
	public FileChangeSyncViewModel ViewModel {
		get;
	}

	private void Window_Loaded(object sender, RoutedEventArgs e) {
		this.ExtendsContentIntoTitleBar = true;
		this.SetTitleBar(this.AppTitleBar);
	}

	private void ApplyItem_Click(object sender, RoutedEventArgs e) {
		if (sender is Button { Tag: FileChangeItem item }) {
			this.ViewModel.ApplySingleCommand.Execute(item);
		}
	}

	private void DiscardItem_Click(object sender, RoutedEventArgs e) {
		if (sender is Button { Tag: FileChangeItem item }) {
			this.ViewModel.DiscardSingleCommand.Execute(item);
		}
	}
}