using System.Collections.Generic;

using MapControl;

using MediaDeck.Composition.Objects;
using MediaDeck.Core.Models.Maps;

using Microsoft.UI.Xaml;

namespace MediaDeck.Views.Panes.ViewerPanes;

public sealed partial class MapViewer {
	private double North {
		get;
		set;
	}

	private double South {
		get;
		set;
	}

	private double West {
		get;
		set;
	}

	private double East {
		get;
		set;
	}

	public BindableReactiveProperty<IEnumerable<MapPin>?> MapPins {
		get;
	} = new();

	public BindableReactiveProperty<GpsLocation> Center {
		get;
	} = new(new(135, 35));


	private ReactiveProperty<int> MapPinSize {
		get;
	} = new(100);

	public MapViewer() {
		this.InitializeComponent();
	}

	private void Map_Loaded(object sender, RoutedEventArgs e) {
		if (this.ViewModel is not { }) {
			return;
		}
		this.UpdateMapControl();
	}

	private void UpdateItemsForMapView() {
		if (this.ViewModel is not { }) {
			return;
		}
		var list = new List<MapPin>();

		foreach (var item in this.ViewModel.MediaContentLibraryViewModel.Files) {
			if (item.Location is not { } location) {
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
				new Rectangle(new((int)viewPoint.X, (int)viewPoint.Y),
					new(this.MapPinSize.Value, this.MapPinSize.Value));

			// 生成した矩形が既に存在するピンとかぶる位置にあるかを確かめて、被るようであれば
			// 被るピンのうち、最も矩形に近いピンに含める。
			// 被らないなら新しいピンを追加する。
			var cores = list.Where(x => rect.IntersectsWith(x.CoreRectangle)).ToList();
			if (cores.Count == 0) {
				list.Add(new MapPin(item.FileModel, rect));
			} else {
				cores.OrderBy(x => rect.DistanceTo(x.CoreRectangle)).First().Items.Add(item.FileModel);
			}
		}

		this.MapPins.Value = list;
	}

	private void UpdateMapControl() {
		this.Map.PointerWheelChanged += (_, _) => {
			if (this.Map is not { } map) {
				return;
			}
			var leftTop = map.ViewToLocation(new(0, 0));
			var rightBottom = map.ViewToLocation(new(map.ActualWidth, map.ActualHeight));
			this.West = leftTop.Longitude;
			this.North = leftTop.Latitude;
			this.East = rightBottom.Longitude;
			this.South = rightBottom.Latitude;
			this.UpdateItemsForMapView();
		};
	}
}