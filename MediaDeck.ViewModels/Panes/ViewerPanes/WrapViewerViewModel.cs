using MediaDeck.Core.Models.Files;

namespace MediaDeck.ViewModels.Panes.ViewerPanes;

[Inject(InjectServiceLifetime.Transient)]
public class WrapViewerViewModel : ViewerPaneViewModelBase {
	public WrapViewerViewModel(FilesManager filesManager) : base("Wrap", filesManager) { }
}