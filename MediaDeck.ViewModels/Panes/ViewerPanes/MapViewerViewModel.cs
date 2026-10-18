using MediaDeck.Composition.Interfaces.MediaItemTypes.ViewModels;
using MediaDeck.Composition.Interfaces.Primitives;
using MediaDeck.Composition.Stores.State.Model;
using MediaDeck.Core.Models.Files;
using MediaDeck.Core.Models.Maps;

namespace MediaDeck.ViewModels.Panes.ViewerPanes;

[Inject(InjectServiceLifetime.Transient)]
public class MapViewerViewModel : ViewerPaneViewModelBase {
	private readonly ILocationFactory _locationFactory;

	public MapViewerViewModel(
		FilesManager filesManager,
		MediaContentLibraryViewModel mediaContentLibraryViewModel,
		SearchConditionManagerViewModel searchConditionManagerViewModel,
		TabStateModel tabState,
		ILocationFactory locationFactory)
		: base(ViewerType.Map, "Map", "\uE800", filesManager, mediaContentLibraryViewModel, searchConditionManagerViewModel, tabState) {
		this._locationFactory = locationFactory;

		this.Center = new BindableReactiveProperty<ILocation>(this._locationFactory.Create(35, 135));

		// 選択ファイルの変化を監視してピンの PinState を更新
		mediaContentLibraryViewModel.SelectedFiles
			.Subscribe(selectedFiles => this.UpdatePinStates(selectedFiles))
			.AddTo(this.CompositeDisposable);
	}

	/// <summary>
	/// 地図上のピンリスト
	/// </summary>
	public BindableReactiveProperty<IEnumerable<MapPinViewModel>?> MapPins {
		get;
	} = new();

	/// <summary>
	/// 地図の中心座標
	/// </summary>
	public BindableReactiveProperty<ILocation> Center {
		get;
	}

	/// <summary>
	/// 地図の表示範囲（緯度経度）
	/// </summary>
	public BindableReactiveProperty<ILocation?> BoundsNorthWest {
		get;
	} = new();

	public BindableReactiveProperty<ILocation?> BoundsSouthEast {
		get;
	} = new();

	/// <summary>
	/// 地図のズームレベル
	/// </summary>
	public BindableReactiveProperty<double> ZoomLevel {
		get;
	} = new(11);

	/// <summary>
	/// ピンが選択されたときに呼び出す。対応するメディアを SelectedFiles に設定し、カメラ位置を更新する。
	/// </summary>
	public void SelectPin(MapPinViewModel pinVm) {
		if (pinVm is null) {
			return;
		}
		// ピンに含まれるメディアをすべて選択状態にする
		this.MediaContentLibraryViewModel.SelectedFiles.Value = pinVm.Items.ToArray();
	}

	/// <summary>
	/// 選択状態に基づいてピンの PinState を更新
	/// </summary>
	private void UpdatePinStates(IMediaItemViewModel[]? selectedFiles) {
		if (this.MapPins.Value is not { } pins) {
			return;
		}
		var selectedSet = selectedFiles?.ToHashSet() ?? [];
		foreach (var pin in pins) {
			var pinItems = pin.Items;
			var selectedCount = pinItems.Count(m => selectedSet.Contains(m));
			pin.PinState.Value = selectedCount switch {
				0 => PinState.Unselected,
				_ when selectedCount == pinItems.Count => PinState.Selected,
				_ => PinState.Indeterminate
			};
		}
	}

	/// <summary>
	/// 表示範囲を更新し、範囲内メディアのみにピンを再生成する。
	/// </summary>
	public void UpdateItemsForMapView(Func<ILocation, System.Drawing.Point> locationToPoint, int pinSize) {
		if (this.BoundsNorthWest.Value is not { } nw || this.BoundsSouthEast.Value is not { } se) {
			return;
		}

		var list = new List<MapPinViewModel>();

		foreach (var item in this.MediaContentLibraryViewModel.Files) {
			if (item.Location is not { } location) {
				continue;
			}

			// 範囲外チェック
			if (
				nw.Latitude < location.Latitude ||
				se.Latitude > location.Latitude ||
				nw.Longitude > location.Longitude ||
				se.Longitude < location.Longitude
			) {
				continue;
			}

			var viewPoint = locationToPoint(location);
			var rect = new Rectangle(viewPoint, new System.Drawing.Size(pinSize, pinSize));

			var cores = list.Where(x => rect.IntersectsWith(x.CoreRectangle)).ToList();
			if (cores.Count == 0) {
				list.Add(new MapPinViewModel(item, rect));
			} else {
				var target = cores.OrderBy(x => rect.DistanceTo(x.CoreRectangle)).First();
				target.AddItem(item);
			}
		}

		// 古いピンを Dispose する
		if (this.MapPins.Value is { } oldPins) {
			foreach (var oldPin in oldPins) {
				oldPin.Dispose();
			}
		}

		this.MapPins.Value = list.ToArray();
		this.UpdatePinStates(this.MediaContentLibraryViewModel.SelectedFiles.Value);
	}

	/// <summary>
	/// すべてのアイテムが表示されるように地図の中心とズームレベルを調整する
	/// </summary>
	/// <param name="mapWidth">地図コントロールの幅</param>
	/// <param name="mapHeight">地図コントロールの高さ</param>
	public void FitToItems(double mapWidth, double mapHeight) {
		var locations = this.MediaContentLibraryViewModel.Files
			.Select(x => x.Location)
			.Where(x => x is { })
			.ToList();

		if (locations.Count == 0) {
			return;
		}

		var minLat = locations.Min(x => x!.Latitude);
		var maxLat = locations.Max(x => x!.Latitude);
		var minLon = locations.Min(x => x!.Longitude);
		var maxLon = locations.Max(x => x!.Longitude);

		this.Center.Value = this._locationFactory.Create((minLat + maxLat) / 2, (minLon + maxLon) / 2);

		if (mapWidth <= 0 || mapHeight <= 0) {
			return;
		}

		double lonDiff = maxLon - minLon;
		if (lonDiff < 0) {
			lonDiff += 360;
		}

		if (lonDiff == 0 && maxLat - minLat == 0) {
			this.ZoomLevel.Value = 15;
			return;
		}

		// 簡易的なズームレベル計算（Webメルカトル投影を考慮）
		// マージンを考慮して 80% の範囲に収める
		const double padding = 0.8;
		const int tileSize = 256;

		// 経度方向のズーム
		double zoomLon = Math.Log(mapWidth * 360.0 / (lonDiff * tileSize / padding), 2);

		// 緯度方向のズーム（メルカトル投影）
		double latRadMin = minLat * Math.PI / 180.0;
		double latRadMax = maxLat * Math.PI / 180.0;
		double latDiffMerc = Math.Log(Math.Tan((latRadMax / 2.0) + (Math.PI / 4.0))) - Math.Log(Math.Tan((latRadMin / 2.0) + (Math.PI / 4.0)));
		double zoomLat = double.IsInfinity(latDiffMerc) || latDiffMerc <= 0
			? 21
			: Math.Log(mapHeight * 2 * Math.PI / (latDiffMerc * tileSize / padding), 2);

		this.ZoomLevel.Value = Math.Clamp(Math.Min(zoomLon, zoomLat), 2, 21);
	}
}