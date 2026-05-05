using MediaDeck.Composition.Stores.State.Model;
using MediaDeck.Core.Models.Files;

namespace MediaDeck.ViewModels.Panes.ViewerPanes;

[Inject(InjectServiceLifetime.Transient)]
public class MapViewerViewModel(FilesManager filesManager) : ViewerPaneViewModelBase(ViewerType.Map, "Map", "\uE800", filesManager);