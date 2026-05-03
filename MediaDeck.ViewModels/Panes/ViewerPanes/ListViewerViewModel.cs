using MediaDeck.Core.Models.Files;

namespace MediaDeck.ViewModels.Panes.ViewerPanes;

[Inject(InjectServiceLifetime.Transient)]
public class ListViewerViewModel : ViewerPaneViewModelBase {
	public ListViewerViewModel(FilesManager filesManager) : base("List", "\uE8FD", filesManager) { }
}