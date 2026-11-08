using CommunityToolkit.WinUI;
using MediaDeck.ViewModels.Panes.ViewerPanes;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.System;
using Windows.UI.Core;

namespace MediaDeck.Views.Panes.ViewerPanes;

/// <summary>
/// 詳細ビューアの View 実装。
/// </summary>
public sealed partial class DetailViewer {
	private const int WheelDeltaThreshold = 120;
	private const double ItemHorizontalMargin = 8.0;
	private const double ScrollSmoothingSpeed = 25.0;
	private readonly SerialDisposable _viewModelDisposable = new();
	private readonly PointerEventHandler _pointerWheelHandler;
	private int _wheelDeltaAccumulator;
	private DateTime _lastWheelTime = DateTime.MinValue;
	private DateTime _lastRenderTime = DateTime.MinValue;
	private bool _isInitialLoaded;
	private bool _isAnimating;
	private double _currentOffset;
	private double _targetOffset;
	private ScrollViewer? _scrollViewer;
	private UIElement? _scrollViewerInnerRoot;
	private ItemsWrapGrid? _wrapGrid;

	/// <summary>
	/// <see cref="DetailViewer"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	public DetailViewer() {
		this.InitializeComponent();
		this._pointerWheelHandler = new PointerEventHandler(this.OnPointerWheelChanged);
		this.Loaded += this.OnLoaded;
		this.Unloaded += this.OnUnloaded;
	}

	/// <summary>
	/// コントロールロード時の初期化処理。
	/// </summary>
	private void OnLoaded(object sender, RoutedEventArgs e) {
		this.AddHandler(PointerWheelChangedEvent, this._pointerWheelHandler, handledEventsToo: false);
		this.SubscribeViewModel(this.ViewModel);
		this.AttachScrollViewerWheelInterceptor();
		this.UpdatePanelSize();

		// 開いた直後に選択アイテムを中央へ配置（先頭なら一番左に自然に収まる）
		this.DispatcherQueue.TryEnqueue(() => {
			this._isInitialLoaded = true;
			this.AttachScrollViewerWheelInterceptor();
			this.UpdatePanelSize();
			if (this.List.SelectedIndex >= 0) {
				this.ScrollToIndexCenter(this.List.SelectedIndex, disableAnimation: true);
			}
		});
	}

	/// <summary>
	/// コントロールアンロード時の破棄処理。
	/// </summary>
	private void OnUnloaded(object sender, RoutedEventArgs e) {
		this.RemoveHandler(PointerWheelChangedEvent, this._pointerWheelHandler);
		this.DetachScrollViewerWheelInterceptor();
		this.StopScrollAnimation();
		this._viewModelDisposable.Disposable = null;
		this._isInitialLoaded = false;
		this._scrollViewer = null;
		this._wrapGrid = null;
	}

	/// <summary>
	/// ScrollViewer 自身の標準ホイールスクロールが発火する前にホイールイベントを捕捉して抑止します。
	/// </summary>
	private void AttachScrollViewerWheelInterceptor() {
		var scrollViewer = this.GetScrollViewer();
		if (scrollViewer == null) {
			return;
		}

		var innerRoot = VisualTreeHelper.GetChildrenCount(scrollViewer) > 0
			? VisualTreeHelper.GetChild(scrollViewer, 0) as UIElement
			: scrollViewer.Content as UIElement;

		if (innerRoot == null || ReferenceEquals(this._scrollViewerInnerRoot, innerRoot)) {
			return;
		}

		this.DetachScrollViewerWheelInterceptor();
		this._scrollViewerInnerRoot = innerRoot;
		this._scrollViewerInnerRoot.AddHandler(PointerWheelChangedEvent, this._pointerWheelHandler, handledEventsToo: false);
	}

	/// <summary>
	/// ScrollViewer 内部ルートからホイールイベントハンドラを解除します。
	/// </summary>
	private void DetachScrollViewerWheelInterceptor() {
		this._scrollViewerInnerRoot?.RemoveHandler(PointerWheelChangedEvent, this._pointerWheelHandler);
		this._scrollViewerInnerRoot = null;
	}

	/// <summary>
	/// ItemsWrapGrid ロード時にパネル参照を保持し、セルサイズを初期化します。
	/// </summary>
	private void WrapGrid_Loaded(object sender, RoutedEventArgs e) {
		this._wrapGrid = (ItemsWrapGrid)sender;
		this.AttachScrollViewerWheelInterceptor();
		this.UpdatePanelSize();
		if (this._isInitialLoaded && this.List.SelectedIndex >= 0) {
			this.ScrollToIndexCenter(this.List.SelectedIndex, disableAnimation: true);
		}
	}

	/// <summary>
	/// ListView / ItemsWrapGrid による選択時の自動 BringIntoView（左端への勝手なスクロール）を抑止します。
	/// </summary>
	private void List_BringIntoViewRequested(UIElement sender, BringIntoViewRequestedEventArgs args) {
		args.Handled = true;
	}

	/// <summary>
	/// ViewModel 変更時に ItemSize の購読を更新します。
	/// </summary>
	/// <param name="oldViewModel">変更前の ViewModel。</param>
	/// <param name="newViewModel">変更後の ViewModel。</param>
	protected override void OnViewModelChanged(DetailViewerViewModel? oldViewModel, DetailViewerViewModel? newViewModel) {
		base.OnViewModelChanged(oldViewModel, newViewModel);
		if (this.IsLoaded) {
			this.SubscribeViewModel(newViewModel);
		}
	}

	/// <summary>
	/// ViewModel の ItemSize 変更を購読します。
	/// </summary>
	/// <param name="viewModel">購読対象の ViewModel。</param>
	private void SubscribeViewModel(DetailViewerViewModel? viewModel) {
		if (viewModel == null) {
			this._viewModelDisposable.Disposable = null;
			return;
		}

		var cd = new CompositeDisposable();
		viewModel.ItemSize
			.Subscribe(_ => this.OnItemSizeChanged())
			.AddTo(cd);

		viewModel.MediaContentLibraryViewModel.Files.CollectionChanged += this.OnFilesCollectionChanged;
		Disposable.Create(() => {
			viewModel.MediaContentLibraryViewModel.Files.CollectionChanged -= this.OnFilesCollectionChanged;
		}).AddTo(cd);

		this._viewModelDisposable.Disposable = cd;
	}

	private void OnFilesCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) {
		if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Reset) {
			this.StopScrollAnimation();
			this._currentOffset = 0;
			this._targetOffset = 0;
			this.GetScrollViewer()?.ChangeView(0, null, null, disableAnimation: true);
		}
	}

	/// <summary>
	/// アイテム表示サイズ変更時にパネルのセルサイズとスクロール位置を更新します。
	/// </summary>
	private void OnItemSizeChanged() {
		if (!this.IsLoaded) {
			return;
		}
		this.UpdatePanelSize();
		this.List.UpdateLayout();
		if (this.List.SelectedIndex >= 0) {
			this.ScrollToIndexCenter(this.List.SelectedIndex, disableAnimation: true);
		}
	}

	/// <summary>
	/// リストの表示領域サイズ変更時に選択アイテムの中央位置を再計算します。
	/// </summary>
	private void List_SizeChanged(object sender, SizeChangedEventArgs e) {
		if (!this._isInitialLoaded || this.List.SelectedIndex < 0) {
			return;
		}
		this.ScrollToIndexCenter(this.List.SelectedIndex, disableAnimation: true);
	}

	/// <summary>
	/// ItemsWrapGrid のセルサイズを現在の ItemSize に揃えます。
	/// </summary>
	private void UpdatePanelSize() {
		var wrapGrid = this.GetWrapGrid();
		if (wrapGrid == null) {
			return;
		}
		var size = this.GetItemSize();
		wrapGrid.ItemWidth = size + ItemHorizontalMargin;
		wrapGrid.ItemHeight = size;
	}

	/// <summary>
	/// 現在のアイテムサイズ（幅・高さ共通）を取得します。
	/// </summary>
	private double GetItemSize() {
		var size = this.ViewModel?.ItemSize.Value ?? 100;
		return size > 0 ? size : 100;
	}

	/// <summary>
	/// リスト内部の ScrollViewer を取得します。
	/// </summary>
	private ScrollViewer? GetScrollViewer() {
		return this._scrollViewer ??= this.List.FindDescendant<ScrollViewer>();
	}

	/// <summary>
	/// リスト内部の ItemsWrapGrid を取得します。
	/// </summary>
	private ItemsWrapGrid? GetWrapGrid() {
		return this._wrapGrid ??= this.List.ItemsPanelRoot as ItemsWrapGrid ?? this.List.FindDescendant<ItemsWrapGrid>();
	}

	/// <summary>
	/// マウスホイール操作ハンドラ。
	/// Ctrlキー押下時はアイテムサイズ変更、通常時は1ノッチにつき1つずつ選択を前後に移動します。
	/// </summary>
	private void OnPointerWheelChanged(object sender, PointerRoutedEventArgs e) {
		if (e.Handled) {
			return;
		}

		var ctrlKeyState = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control);
		if (ctrlKeyState.HasFlag(CoreVirtualKeyStates.Down)) {
			// Ctrl+ホイール: サムネイルサイズ変更
			this._wheelDeltaAccumulator = 0;
			var delta = e.GetCurrentPoint(this).Properties.MouseWheelDelta;
			this.ViewModel?.AdjustItemSize(delta);
			e.Handled = true;
			return;
		}

		var wheelDelta = e.GetCurrentPoint(this).Properties.MouseWheelDelta;
		if (wheelDelta == 0) {
			return;
		}

		// デフォルトの水平スクロール（ScrollViewer による勝手な横移動）を抑止
		e.Handled = true;

		this.HandleWheelStep(wheelDelta);
	}

	/// <summary>
	/// ホイールの回転デルタを累積し、閾値に達するごとに選択を移動します。
	/// </summary>
	/// <param name="delta">ホイールデルタ値。</param>
	private void HandleWheelStep(int delta) {
		var now = DateTime.UtcNow;
		if ((now - this._lastWheelTime).TotalMilliseconds > 300) {
			this._wheelDeltaAccumulator = 0;
		}
		this._lastWheelTime = now;

		// 回転方向が逆転した場合はアキュムレータをリセット
		if ((this._wheelDeltaAccumulator > 0 && delta < 0) || (this._wheelDeltaAccumulator < 0 && delta > 0)) {
			this._wheelDeltaAccumulator = 0;
		}

		this._wheelDeltaAccumulator += delta;

		int steps = 0;
		// 下スクロール (delta < 0) で右に移動 (Index + 1)
		while (this._wheelDeltaAccumulator <= -WheelDeltaThreshold) {
			this._wheelDeltaAccumulator += WheelDeltaThreshold;
			steps++;
		}

		// 上スクロール (delta > 0) で左に移動 (Index - 1)
		while (this._wheelDeltaAccumulator >= WheelDeltaThreshold) {
			this._wheelDeltaAccumulator -= WheelDeltaThreshold;
			steps--;
		}

		if (steps != 0) {
			this.MoveSelection(steps);
		}
	}

	/// <summary>
	/// 選択インデックスを指定ステップ数移動します。
	/// </summary>
	/// <param name="steps">移動ステップ数 (+: 右/次へ, -: 左/前へ)。</param>
	private void MoveSelection(int steps) {
		var count = this.List.Items.Count;
		if (count == 0) {
			return;
		}

		var currentIndex = this.List.SelectedIndex;
		int targetIndex;

		if (currentIndex < 0) {
			targetIndex = steps > 0 ? 0 : count - 1;
		} else {
			targetIndex = currentIndex + steps;
		}

		// 範囲外なら境界で停止
		targetIndex = Math.Clamp(targetIndex, 0, count - 1);

		if (targetIndex == currentIndex) {
			return;
		}

		this.List.SelectedIndex = targetIndex;
	}

	/// <inheritdoc />
	protected override void List_SelectionChanged(object sender, SelectionChangedEventArgs e) {
		base.List_SelectionChanged(sender, e);
		if (sender is not ListView listView) {
			return;
		}
		if (listView.SelectedIndex == -1) {
			return;
		}

		var disableAnimation = !this._isInitialLoaded;
		this.ScrollToIndexCenter(listView.SelectedIndex, disableAnimation);
	}

	/// <summary>
	/// 指定されたインデックスのアイテムをリストの中央にスクロールします。
	/// ItemsWrapGrid の均一セル幅(stride)からインデックスの絶対オフセットを一意に計算し、
	/// 先頭付近は左端(0)、末尾付近は右端にクランプします。
	/// </summary>
	/// <param name="index">対象のインデックス。</param>
	/// <param name="disableAnimation">アニメーションを無効化するかどうか。</param>
	private void ScrollToIndexCenter(int index, bool disableAnimation) {
		var count = this.List.Items.Count;
		if (index < 0 || index >= count) {
			return;
		}

		var scrollViewer = this.GetScrollViewer();
		if (scrollViewer == null) {
			return;
		}

		var viewportWidth = scrollViewer.ViewportWidth > 0 ? scrollViewer.ViewportWidth : this.List.ActualWidth;
		if (viewportWidth <= 0) {
			return;
		}

		var itemSize = this.GetItemSize();
		var stride = itemSize + ItemHorizontalMargin;
		var paddingLeft = this.List.Padding.Left;
		var paddingRight = this.List.Padding.Right;

		var itemCenter = paddingLeft + (index * stride) + (stride / 2.0);
		var totalExtent = paddingLeft + paddingRight + (count * stride);
		var maxOffset = Math.Max(0.0, totalExtent - viewportWidth);
		var targetOffset = Math.Clamp(itemCenter - (viewportWidth / 2.0), 0.0, maxOffset);

		if (disableAnimation) {
			this.StopScrollAnimation();
			this._currentOffset = targetOffset;
			this._targetOffset = targetOffset;
			scrollViewer.ChangeView(targetOffset, null, null, disableAnimation: true);
			return;
		}

		this._targetOffset = targetOffset;
		this.StartScrollAnimation();
	}

	/// <summary>
	/// フレーム単位のスムーズスクロールアニメーションを開始します。
	/// </summary>
	private void StartScrollAnimation() {
		if (this._isAnimating) {
			return;
		}
		this._isAnimating = true;
		this._lastRenderTime = DateTime.UtcNow;
		CompositionTarget.Rendering += this.OnCompositionTargetRendering;
	}

	/// <summary>
	/// フレーム単位のスムーズスクロールアニメーションを停止します。
	/// </summary>
	private void StopScrollAnimation() {
		if (!this._isAnimating) {
			return;
		}
		this._isAnimating = false;
		CompositionTarget.Rendering -= this.OnCompositionTargetRendering;
	}

	/// <summary>
	/// 毎フレーム現在オフセットを目標オフセットへ指数減衰で補間し、ScrollViewer を同期更新します。
	/// </summary>
	private void OnCompositionTargetRendering(object? sender, object e) {
		var scrollViewer = this.GetScrollViewer();
		if (scrollViewer == null) {
			this.StopScrollAnimation();
			return;
		}

		var now = DateTime.UtcNow;
		var dt = Math.Clamp((now - this._lastRenderTime).TotalSeconds, 0.001, 0.05);
		this._lastRenderTime = now;

		var diff = this._targetOffset - this._currentOffset;
		if (Math.Abs(diff) < 0.5) {
			this._currentOffset = this._targetOffset;
			scrollViewer.ChangeView(this._targetOffset, null, null, disableAnimation: true);
			this.StopScrollAnimation();
			return;
		}

		var factor = 1.0 - Math.Exp(-ScrollSmoothingSpeed * dt);
		this._currentOffset += diff * factor;
		scrollViewer.ChangeView(this._currentOffset, null, null, disableAnimation: true);
	}
}