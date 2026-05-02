using MediaDeck.Services;
using MediaDeck.ViewModels;
using MediaDeck.Views.Dialogs;

using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using WinRT.Interop;

namespace MediaDeck.Views;

[Inject(InjectServiceLifetime.Transient)]
public sealed partial class MainWindow {
	private readonly MainWindowViewModel _viewModel;
	private readonly WindowManager _windowManager;

	private readonly System.Reactive.Disposables.CompositeDisposable _disposable = new();

	/// <summary>
	/// タブ切り離し中に作成された新ウィンドウのAppWindow.Idを一時保持する。
	/// TabTearOutWindowRequested と TabTearOutRequested の間で受け渡しに使用。
	/// </summary>
	private WindowId? _tearOutWindowId;

	public MainWindow(MainWindowViewModel viewModel, WindowManager windowManager) {
		this._viewModel = viewModel;
		this._windowManager = windowManager;
		this.InitializeComponent();

		this._viewModel.SelectedTab.Subscribe(tab => {
			if (tab != null && this.MainTabView.SelectedItem != tab) {
				this.MainTabView.SelectedItem = tab;
			}
		}).AddTo(this._disposable);

		this.Closed += (s, e) => this._disposable.Dispose();
	}

	private void MainTabView_TabCloseRequested(TabView sender, TabViewTabCloseRequestedEventArgs args) {
		if (args.Item is TabContext tabContext) {
			this._viewModel.CloseTab(tabContext);
		}
	}

	private void MainTabView_SelectionChanged(object sender, SelectionChangedEventArgs e) {
		if (this.MainTabView.SelectedItem is TabContext tabContext) {
			if (this._viewModel.SelectedTab.Value != tabContext) {
				this._viewModel.SelectedTab.Value = tabContext;
			}
		}
	}

	private void Window_Loaded(object sender, RoutedEventArgs e) {
		this._viewModel.WindowActivatedCommand.Execute(Unit.Default);
	}

	/// <summary>
	/// タブがTabViewから切り離し開始された際に呼ばれる。
	/// 新しいウィンドウを作成し、そのWindowIdをイベント引数に設定する。
	/// </summary>
	private void MainTabView_TabTearOutWindowRequested(TabView sender, TabViewTabTearOutWindowRequestedEventArgs args) {
		this._tearOutWindowId = this._windowManager.CreateWindowForTearOut();
		args.NewWindowId = this._tearOutWindowId.Value;
	}

	/// <summary>
	/// 新しいウィンドウが作成された後に呼ばれる。
	/// タブを元のウィンドウから切り離し、新しいウィンドウへ移動する。
	/// </summary>
	private void MainTabView_TabTearOutRequested(TabView sender, TabViewTabTearOutRequestedEventArgs args) {
		if (this._tearOutWindowId == null) {
			return;
		}

		// args.Tabs には TabViewItem が含まれる。DataContext から TabContext を取得する。
		foreach (var tab in args.Tabs) {
			if (tab is TabViewItem tabViewItem && tabViewItem.DataContext is TabContext tabContext) {
				this._windowManager.MoveTabToWindow(
					tabContext.TabState,
					this._viewModel.WindowId,
					this._tearOutWindowId.Value);
			}
		}

		this._tearOutWindowId = null;
	}

	/// <summary>
	/// 外部から切り離されたタブがこのTabView上にドラッグされた際に呼ばれる。
	/// ドロップを許可するかどうかを設定する。
	/// </summary>
	private void MainTabView_ExternalTornOutTabsDropping(TabView sender, TabViewExternalTornOutTabsDroppingEventArgs args) {
		// 同一アプリのタブは常に受け入れ可能
		args.AllowDrop = true;
	}

	/// <summary>
	/// 外部から切り離されたタブがこのTabViewにドロップされた際に呼ばれる。
	/// 元ウィンドウからタブを削除し、このウィンドウのTabViewに挿入する。
	/// </summary>
	private void MainTabView_ExternalTornOutTabsDropped(TabView sender, TabViewExternalTornOutTabsDroppedEventArgs args) {
		// このウィンドウのAppWindow.Idを取得
		var targetAppWindowId = GetAppWindowId(this);

		foreach (var tab in args.Tabs) {
			if (tab is TabViewItem tabViewItem && tabViewItem.DataContext is TabContext tabContext) {
				// ソースウィンドウは WindowManager が TabStateModel の所属先を自動検索する
				this._windowManager.MoveTabToWindow(
					tabContext.TabState,
					targetAppWindowId,
					args.DropIndex);
			}
		}
	}

	/// <summary>
	/// WindowオブジェクトからAppWindow.Idを取得するヘルパー。
	/// </summary>
	private static WindowId GetAppWindowId(Window window) {
		var hWnd = WindowNative.GetWindowHandle(window);
		return Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
	}

	private async void TabHeader_DoubleTapped(object sender, Microsoft.UI.Xaml.Input.DoubleTappedRoutedEventArgs e) {
		if (sender is FrameworkElement fe && fe.DataContext is TabContext tabContext) {
			await this.ShowRenameTabDialogAsync(tabContext);
			e.Handled = true;
		}
	}

	private async void TabViewItem_KeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e) {
		if (e.Key == Windows.System.VirtualKey.F2) {
			if (sender is FrameworkElement fe && fe.DataContext is TabContext tabContext) {
				await this.ShowRenameTabDialogAsync(tabContext);
				e.Handled = true;
			}
		}
	}

	private async System.Threading.Tasks.Task ShowRenameTabDialogAsync(TabContext tabContext) {
		var dialog = new TabRenameDialog(tabContext.TabState.DisplayName.Value) {
			XamlRoot = this.Content.XamlRoot
		};

		var result = await dialog.ShowAsync();
		if (result == ContentDialogResult.Primary && !string.IsNullOrWhiteSpace(dialog.ResultText)) {
			tabContext.TabState.DisplayName.Value = dialog.ResultText;
		}
	}
}