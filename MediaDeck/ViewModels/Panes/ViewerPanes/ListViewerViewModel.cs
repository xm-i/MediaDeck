using MediaDeck.Models.FileDetailManagers;

namespace MediaDeck.ViewModels.Panes.ViewerPanes;

[Inject(InjectServiceLifetime.Transient)]
public class ListViewerViewModel : ViewerPaneViewModelBase {
	public ListViewerViewModel(FilesManager filesManager) : base ("List", filesManager){
	}
}
