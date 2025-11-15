using MediaDeck.Models.FileDetailManagers;

namespace MediaDeck.ViewModels.Panes.ViewerPanes;

[AddTransient]
public class WrapViewerViewModel : ViewerPaneViewModelBase {
	public WrapViewerViewModel(FilesManager filesManager) : base ("Wrap", filesManager){
	}
}
