using MediaDeck.Composition.Stores.State.Model;
using MediaDeck.Core.Models.Files;

namespace MediaDeck.ViewModels.Panes.ViewerPanes;

[Inject(InjectServiceLifetime.Transient)]
public class WrapViewerViewModel(FilesManager filesManager) : ViewerPaneViewModelBase(ViewerType.Wrap, "Wrap", "\uE80A", filesManager);