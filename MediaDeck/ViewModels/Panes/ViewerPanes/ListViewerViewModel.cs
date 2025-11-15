using MediaDeck.Models.FileDetailManagers;

namespace MediaDeck.ViewModels.Panes.ViewerPanes;

[AddTransient]
public class ListViewerViewModel : ViewerPaneViewModelBase {
	public ListViewerViewModel(FilesManager filesManager) : base ("List", filesManager){
	}
}
