using System.Collections.Generic;

using MapControl;

using MediaDeck.Composition.Objects;
using MediaDeck.Models.Files;

namespace MediaDeck.Models.Maps;

[AddTransient]
public class MapModel {
	private Map? _map;
	public MapModel(MediaContentLibrary mediaContentLibrary) {
		this.MediaContentLibrary = mediaContentLibrary;
	}

	public ReactiveProperty<int> MapPinSize {
		get;
	} = new(100);

	public ReactiveProperty<double> West {
		get;
	} = new();

	public ReactiveProperty<double> East {
		get;
	} = new();

	public ReactiveProperty<double> North {
		get;
	} = new();

	public ReactiveProperty<double> South {
		get;
	} = new();

	public MediaContentLibrary MediaContentLibrary {
		get;
	}

	public ReactiveProperty<IEnumerable<MapPin>?> MapPins {
		get;
	} = new();

	public ReactiveProperty<GpsLocation> Center {
		get;
	} = new(new(35, 135));

	public void UpdateItemsForMapView() {
		if(this._map is not {} map) {
			return;
		}
		var list = new List<MapPin>();

		foreach (var item in this.MediaContentLibrary.Files) {
			if (!(item.Location is { } location)) {
				continue;
			}

			if (
				this.North.Value < location.Latitude ||
				this.South.Value > location.Latitude ||
				this.West.Value > location.Longitude ||
				this.East.Value < location.Longitude
				) {
				continue;
			}

			var topLeft = new Location(location.Latitude, location.Longitude);
			if (map.LocationToView(topLeft) is not { } viewPoint ) {
				continue;
			}

			// 座標とピンサイズから矩形を生成
			var rect =
				new Rectangle(
					viewPoint,
					new System.Drawing.Size(this.MapPinSize.Value, this.MapPinSize.Value)
				);

			// 生成した矩形が既に存在するピンとかぶる位置にあるかを確かめて、被るようであれば
			// 被るピンのうち、最も矩形に近いピンに含める。
			// 被らないなら新しいピンを追加する。
			var cores = list.Where(x => rect.IntersectsWith(x.CoreRectangle)).ToList();
			if (!cores.Any()) {
				list.Add(new MapPin(item, rect));
			} else {
				cores.OrderBy(x => rect.DistanceTo(x.CoreRectangle)).First().Items.Add(item);
			}
		}

		this.MapPins.Value = list;
	}

	public void UpdateMapControl(Map map) {
		this._map = map;
		this._map.PointerWheelChanged += (sender, args) => {
			if (this._map is not { } map) {
				return;
			}
			var leftTop = map.ViewToLocation(new Point(0,0));
			var rightBottom = map.ViewToLocation(new Point(map.ActualWidth, map.ActualHeight));
			this.West.Value = leftTop.Longitude;
			this.North.Value = leftTop.Latitude;
			this.East.Value = rightBottom.Longitude;
			this.South.Value = rightBottom.Latitude;
			this.UpdateItemsForMapView();
		};
	}
}
