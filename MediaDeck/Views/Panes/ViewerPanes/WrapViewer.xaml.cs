using MediaDeck.ViewModels.Panes.ViewerPanes;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace MediaDeck.Views.Panes.ViewerPanes;

/// <summary>
/// メディアアイテムをグリッド状に表示するビュー。
/// 表示領域に合わせてアイテム幅を調整し、エクスプローラーのように均等な余白を実現します。
/// </summary>
public sealed partial class WrapViewer {
	private readonly SerialDisposable _viewModelDisposable = new();
	private readonly PointerEventHandler _pointerWheelHandler;
	private ItemsWrapGrid? _wrapGrid;

	public WrapViewer() {
		this.InitializeComponent();
		this._pointerWheelHandler = new PointerEventHandler(this.HandleListPointerWheelChanged);
		this.Loaded += this.OnLoaded;
		this.Unloaded += this.OnUnloaded;
	}

	/// <summary>
	/// コントロールがロードされた際の処理。イベントハンドラを登録し、ViewModelの購読を開始します。
	/// </summary>
	/// <param name="sender">イベント発生元。</param>
	/// <param name="e">イベント引数。</param>
	private void OnLoaded(object sender, RoutedEventArgs e) {
		this.List.AddHandler(PointerWheelChangedEvent, this._pointerWheelHandler, true);
		this.SubscribeViewModel(this.ViewModel);
		this.UpdateItemWidth();
	}

	/// <summary>
	/// コントロールがアンロードされた際の処理。多重登録やリークを防ぐためイベントハンドラおよびViewModelの購読を解除します。
	/// </summary>
	/// <param name="sender">イベント発生元。</param>
	/// <param name="e">イベント引数。</param>
	private void OnUnloaded(object sender, RoutedEventArgs e) {
		this.List.RemoveHandler(PointerWheelChangedEvent, this._pointerWheelHandler);
		this._viewModelDisposable.Disposable = null;
	}

	/// <summary>
	/// ViewModelが変更された際の処理。ItemSizeの変更を購読します。
	/// </summary>
	/// <param name="oldViewModel">古いViewModel。</param>
	/// <param name="newViewModel">新しいViewModel。</param>
	protected override void OnViewModelChanged(ViewerSelectorViewModel? oldViewModel, ViewerSelectorViewModel? newViewModel) {
		base.OnViewModelChanged(oldViewModel, newViewModel);
		if (this.IsLoaded) {
			this.SubscribeViewModel(newViewModel);
		}
	}

	/// <summary>
	/// ViewModelのプロパティ変更を購読します。古い購読は自動的に破棄されます。
	/// </summary>
	/// <param name="viewModel">購読対象のViewModel。</param>
	private void SubscribeViewModel(ViewerSelectorViewModel? viewModel) {
		this._viewModelDisposable.Disposable = viewModel?.ItemSize
			.Subscribe(_ => this.UpdateItemWidth());
	}

	/// <summary>
	/// パネルがロードされた際の処理。パネルの参照を保持し、アイテム幅を更新します。
	/// </summary>
	/// <param name="sender">イベント発生元。</param>
	/// <param name="e">イベント引数。</param>
	private void WrapGrid_Loaded(object sender, RoutedEventArgs e) {
		this._wrapGrid = (ItemsWrapGrid)sender;
		this.UpdateItemWidth();
	}

	/// <summary>
	/// リストのサイズが変更された際の処理。アイテム幅を更新します。
	/// </summary>
	/// <param name="sender">イベント発生元。</param>
	/// <param name="e">イベント引数。</param>
	private void List_SizeChanged(object sender, SizeChangedEventArgs e) {
		this.UpdateItemWidth();
	}

	/// <summary>
	/// 表示領域に合わせてアイテムの幅を動的に計算・設定します。
	/// これによりアイテム間の幅が均等に広がります。
	/// </summary>
	private void UpdateItemWidth() {
		if (this._wrapGrid == null || this.ViewModel == null) {
			return;
		}

		double availableWidth = this.List.ActualWidth - this.List.Padding.Left - this.List.Padding.Right;
		if (availableWidth <= 0) {
			return;
		}

		// アイテムの最小幅（スライダーの値 + マージン分）
		// MediaCardStyle の Margin="4" なので左右で 8px。少し余裕を持たせて 12px とする。
		double minWidth = this.ViewModel.ItemSize.Value + 12;
		int columns = (int)Math.Max(1, Math.Floor(availableWidth / minWidth));

		// 1列あたりの幅を計算してパネルに設定
		this._wrapGrid.ItemWidth = availableWidth / columns;
	}
}