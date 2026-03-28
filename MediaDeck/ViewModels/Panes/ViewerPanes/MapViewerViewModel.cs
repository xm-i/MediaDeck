using MediaDeck.Core.Models.FileDetailManagers;

namespace MediaDeck.ViewModels.Panes.ViewerPanes;

[Inject(InjectServiceLifetime.Transient)]
public class MapViewerViewModel : ViewerPaneViewModelBase {
	public MapViewerViewModel(FilesManager filesManager) : base ("Map", filesManager){
	}
}
