using System.Collections.Generic;

using MapControl;

using MediaDeck.Composition.Objects;
using MediaDeck.Models.FileDetailManagers;
using MediaDeck.Models.Maps;

namespace MediaDeck.ViewModels.Panes.ViewerPanes;

[Inject(InjectServiceLifetime.Transient)]
public class MapViewerViewModel : ViewerPaneViewModelBase {
	private readonly MapModel _mapModel;
	public MapViewerViewModel(FilesManager filesManager,MapModel mapModel) : base ("Map", filesManager){
		this._mapModel = mapModel;
		this.MapPins = this._mapModel.MapPins.ToBindableReactiveProperty();
		this.Center = this._mapModel.Center.ToBindableReactiveProperty();
	}

	public BindableReactiveProperty<IEnumerable<MapPin>?> MapPins {
		get;
	}

	public BindableReactiveProperty<GpsLocation> Center {
		get;
	}

	public void UpdateMapControl(Map map) {
		this._mapModel.UpdateMapControl(map);
	}
}
