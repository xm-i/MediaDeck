using System.Collections.Generic;

using MapControl;

using MediaDeck.Composition.Objects;
using MediaDeck.Core.Models.Maps;

using Microsoft.UI.Xaml;

namespace MediaDeck.Views.Panes.ViewerPanes;

public sealed partial class MapViewer : ViewerPaneBase {
	public double North {
		get;
		set;
	}

	public double South {
		get;
		set;
	}

	public double West {
		get;
		set;
	}

	public double East {
		get;
		set;
	}

	public BindableReactiveProperty<IEnumerable<MapPin>?> MapPins {
		get;
	} = new();

	public BindableReactiveProperty<GpsLocation> Center {
		get;
	} = new(new(135, 35));


	public ReactiveProperty<int> MapPinSize {
		get;
	} = new(100);

	public MapViewer() {
		this.InitializeComponent();
	}

	private void Map_Loaded(object sender, RoutedEventArgs e) {
		if (this.ViewModel is not { } vm) {
			return;
		}
		this.UpdateMapControl();
	}

	public void UpdateItemsForMapView() {
		if (this.ViewModel is not { } vm) {
			return;
		}
		var list = new List<MapPin>();

		foreach (var item in this.ViewModel.MediaContentLibraryViewModel.Files) {
			if (!(item.Location is { } location)) {
				continue;
			}

			if (
				this.North < location.Latitude ||
				this.South > location.Latitude ||
				this.West > location.Longitude ||
				this.East < location.Longitude
			) {
				continue;
			}

			var topLeft = new Location(location.Latitude, location.Longitude);
			if (this.Map.LocationToView(topLeft) is not { } viewPoint) {
				continue;
			}

			// 座標とピンサイズから矩形を生成
			var rect =
				new Rectangle(new System.Drawing.Point((int)viewPoint.X, (int)viewPoint.Y),
					new System.Drawing.Size(this.MapPinSize.Value, this.MapPinSize.Value));

			// 生成した矩形が既に存在するピンとかぶる位置にあるかを確かめて、被るようであれば
			// 被るピンのうち、最も矩形に近いピンに含める。
			// 被らないなら新しいピンを追加する。
			var cores = list.Where(x => rect.IntersectsWith(x.CoreRectangle)).ToList();
			if (!cores.Any()) {
				list.Add(new MapPin(item.FileModel, rect));
			} else {
				cores.OrderBy(x => rect.DistanceTo(x.CoreRectangle)).First().Items.Add(item.FileModel);
			}
		}

		this.MapPins.Value = list;
	}

	public void UpdateMapControl() {
		this.Map.PointerWheelChanged += (sender, args) => {
			if (this.Map is not { } map) {
				return;
			}
			var leftTop = map.ViewToLocation(new Point(0, 0));
			var rightBottom = map.ViewToLocation(new Point(map.ActualWidth, map.ActualHeight));
			this.West = leftTop.Longitude;
			this.North = leftTop.Latitude;
			this.East = rightBottom.Longitude;
			this.South = rightBottom.Latitude;
			this.UpdateItemsForMapView();
		};
	}
}