using MediaDeck.Composition.Stores.State.Model;
using MediaDeck.Core.Models.Files;

namespace MediaDeck.ViewModels.Panes.ViewerPanes;

/// <summary>
/// メディアアイテムをグリッド状に折り返して並べるビューの ViewModel。
/// </summary>
[Inject(InjectServiceLifetime.Transient)]
public class WrapViewerViewModel(
	FilesManager filesManager,
	MediaContentLibraryViewModel mediaContentLibraryViewModel,
	SearchConditionManagerViewModel searchConditionManagerViewModel,
	TabStateModel tabState)
	: ViewerPaneViewModelBase(ViewerType.Wrap, "Wrap", "\uE80A", filesManager, mediaContentLibraryViewModel, searchConditionManagerViewModel, tabState);