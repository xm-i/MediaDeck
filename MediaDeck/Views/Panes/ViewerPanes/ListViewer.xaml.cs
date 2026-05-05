using CommunityToolkit.WinUI;
using MediaDeck.ViewModels.Panes.ViewerPanes;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;

namespace MediaDeck.Views.Panes.ViewerPanes;

/// <summary>
/// メディア一覧のリスト表示ビュー。
///
/// このクラスは以下の責務を持つ:
/// 1) ListView本体の横スクロール量をヘッダーに同期する
/// 2) ヘッダー境界ドラッグによるカラム幅変更を ViewModel に反映する
/// 3) ヘッダー右クリックで列表示ON/OFFメニューを表示する
///
/// カラム状態の永続化は ViewModel 側 (TabStateModel.ViewerState) で実施する。
/// </summary>
public sealed partial class ListViewer {
	/// <summary>
	/// ListView テンプレートから取得した ScrollViewer。
	/// ヘッダーの横スクロール同期に使用する。
	/// </summary>
	private ScrollViewer? _bodyScrollViewer;

	public ListViewer() {
		this.InitializeComponent();
		this.Loaded += this.OnLoaded;
	}

	/// <summary>
	/// 初期ロード時にイベント接続を行う。
	/// </summary>
	private void OnLoaded(object sender, RoutedEventArgs e) {
		// Ctrl+ホイールでサムネイルサイズを変更するため、ポインタホイールを捕捉する。
		this.List.AddHandler(PointerWheelChangedEvent, new PointerEventHandler(this.HandleListPointerWheelChanged), true);

		// ListView 内部の ScrollViewer を取得し、横スクロール同期イベントを登録する。
		this._bodyScrollViewer = this.List.FindDescendant<ScrollViewer>();
		if (this._bodyScrollViewer is { } sv) {
			sv.ViewChanged += this.BodyScrollViewer_ViewChanged;
		}
	}

	/// <summary>
	/// 本文横スクロール時にヘッダーも同じオフセットへ同期する。
	/// </summary>
	private void BodyScrollViewer_ViewChanged(object? sender, ScrollViewerViewChangedEventArgs e) {
		if (sender is ScrollViewer sv) {
			this.HeaderScrollViewer.ChangeView(sv.HorizontalOffset, null, null, true);
		}
	}

	/// <summary>
	/// ヘッダー境界ドラッグ時にカラム幅を更新する。
	/// </summary>
	private void HeaderResizer_DragDelta(object sender, DragDeltaEventArgs e) {
		if (sender is FrameworkElement { Tag: ListViewerColumn column }) {
			var newWidth = Math.Max(column.MinWidth, column.Width.Value + e.HorizontalChange);
			column.Width.Value = newWidth;
		}
	}

	/// <summary>
	/// リサイズ境界にマウスが入った際、左右リサイズカーソルを表示する。
	/// </summary>
	private void HeaderResizer_PointerEntered(object sender, PointerRoutedEventArgs e) {
		this.ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast);
	}

	/// <summary>
	/// リサイズ境界からマウスが出た際、カーソル表示を元に戻す。
	/// </summary>
	private void HeaderResizer_PointerExited(object sender, PointerRoutedEventArgs e) {
		this.ProtectedCursor = null;
	}

	/// <summary>
	/// ヘッダー右クリックで列表示ON/OFFメニューを表示する。
	/// </summary>
	private void HeaderGrid_RightTapped(object sender, RightTappedRoutedEventArgs e) {
		if (this.ViewModel?.ListViewerViewModel is not { } listVm) {
			return;
		}
		if (sender is not FrameworkElement element) {
			return;
		}
		var flyout = new MenuFlyout();
		foreach (var column in listVm.AllColumns) {
			if (!column.CanHide) {
				continue;
			}
			var capturedId = column.Id;
			var item = new ToggleMenuFlyoutItem {
				Text = string.IsNullOrEmpty(column.Header) ? capturedId : column.Header,
				IsChecked = column.IsVisible.Value,
			};
			item.Click += (_, _) => listVm.ToggleColumnVisibility(capturedId);
			flyout.Items.Add(item);
		}
		if (flyout.Items.Count > 0) {
			flyout.ShowAt(element, e.GetPosition(element));
			e.Handled = true;
		}
	}
}