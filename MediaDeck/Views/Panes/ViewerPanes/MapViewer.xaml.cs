using System.Collections.Specialized;
using MapControl;
using MediaDeck.Objects.Maps;
using MediaDeck.ViewModels.Panes.ViewerPanes;
using Microsoft.UI.Xaml;

namespace MediaDeck.Views.Panes.ViewerPanes;

/// <summary>
/// メディアアイテムをマップ上に表示するビュー。
/// </summary>
public sealed partial class MapViewer {
	private readonly Subject<Unit> PinUpdateRequest = new();
	private readonly CompositeDisposable _disposables = new();
	private readonly SerialDisposable _filesSubscription = new();

	public MapViewer() {
		this.InitializeComponent();
		this.PinUpdateRequest
			.Debounce(TimeSpan.FromMilliseconds(300))
			.ObserveOnCurrentSynchronizationContext()
			.Subscribe(x => {
				if (this.Map is not { } map) {
					return;
				}
				if (this.ViewModel is not { } mapVm) {
					return;
				}
				var leftTop = map.ViewToLocation(new(0, 0));
				var rightBottom = map.ViewToLocation(new(map.ActualWidth, map.ActualHeight));

				mapVm.BoundsNorthWest.Value = new MediaDeckLocation(leftTop.Latitude, leftTop.Longitude);
				mapVm.BoundsSouthEast.Value = new MediaDeckLocation(rightBottom.Latitude, rightBottom.Longitude);

				mapVm.UpdateItemsForMapView(loc => {
					var viewPoint = map.LocationToView(new Location(loc.Latitude, loc.Longitude));
					return new System.Drawing.Point((int)viewPoint.X, (int)viewPoint.Y);
				}, 100);
			}).AddTo(this._disposables);
	}

	/// <summary>
	/// マップコントロールがロードされた際の処理。イベントを接続し初期状態にフィットさせます。
	/// </summary>
	/// <param name="sender">イベント発生元。</param>
	/// <param name="e">イベント引数。</param>
	private void Map_Loaded(object sender, RoutedEventArgs e) {
		this.Map.ViewportChanged -= this.Map_ViewportChanged;
		this.Map.ViewportChanged += this.Map_ViewportChanged;

		if (this.ViewModel is not { } vm) {
			return;
		}

		// 初期状態で全アイテムが入るように調整
		// Files が空でない場合は即座に、空の場合は最初の更新を待つ
		if (vm.MediaContentLibraryViewModel.Files.Count > 0) {
			vm.FitToItems(this.Map.ActualWidth, this.Map.ActualHeight);
		}

		this.SubscribeFiles(vm);
	}

	/// <summary>
	/// マップコントロールがアンロードされた際の処理。イベントハンドラおよびコレクション購読を解除します。
	/// </summary>
	/// <param name="sender">イベント発生元。</param>
	/// <param name="e">イベント引数。</param>
	private void Map_Unloaded(object sender, RoutedEventArgs e) {
		this.Map.ViewportChanged -= this.Map_ViewportChanged;
		this._filesSubscription.Disposable = null;
	}

	/// <summary>
	/// ViewModelが変更された際の処理。新しいViewModelのファイルコレクションを購読します。
	/// </summary>
	/// <param name="oldViewModel">古いViewModel。</param>
	/// <param name="newViewModel">新しいViewModel。</param>
	protected override void OnViewModelChanged(MapViewerViewModel? oldViewModel, MapViewerViewModel? newViewModel) {
		base.OnViewModelChanged(oldViewModel, newViewModel);
		if (this.Map.IsLoaded) {
			this.SubscribeFiles(newViewModel);
			if (newViewModel?.MediaContentLibraryViewModel.Files.Count > 0) {
				newViewModel.FitToItems(this.Map.ActualWidth, this.Map.ActualHeight);
			}
		}
	}

	/// <summary>
	/// ファイルコレクションの変更を購読します。古い購読は自動的に破棄されます。
	/// </summary>
	/// <param name="vm">ViewModel。</param>
	private void SubscribeFiles(MapViewerViewModel? vm) {
		if (vm?.MediaContentLibraryViewModel?.Files is { } files) {
			this._filesSubscription.Disposable = Observable.FromEvent<NotifyCollectionChangedEventHandler, NotifyCollectionChangedEventArgs>(
				h => (s, e) => h(e),
				h => files.CollectionChanged += h,
				h => files.CollectionChanged -= h
			).Subscribe(_ => {
				this.DispatcherQueue.TryEnqueue(() => {
					this.PinUpdateRequest.OnNext(Unit.Default);
				});
			});
		} else {
			this._filesSubscription.Disposable = null;
		}
	}

	/// <summary>
	/// ピンがタップされた際の処理。タップされたピンを選択状態にします。
	/// </summary>
	/// <param name="sender">イベント発生元。</param>
	/// <param name="e">イベント引数。</param>
	private void Pin_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e) {
		if (this.ViewModel is not { } vm) {
			return;
		}
		var pinVm = (MapPinViewModel)((FrameworkElement)sender).DataContext;
		vm.SelectPin(pinVm);
	}

	/// <summary>
	/// マップのビューポート変更時のイベントハンドラ。ピン更新リクエストを発行します。
	/// </summary>
	/// <param name="sender">イベント発生元。</param>
	/// <param name="e">イベント引数。</param>
	private void Map_ViewportChanged(object? sender, EventArgs e) {
		this.PinUpdateRequest.OnNext(Unit.Default);
	}
}