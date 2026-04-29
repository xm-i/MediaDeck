using MediaDeck.Composition.Enum;
using MediaDeck.MediaItemTypes.Base.ViewModels;

namespace MediaDeck.MediaItemTypes.FolderGroup.ViewModels;

/// <summary>
/// フォルダグループのメディアアイテムViewModel
/// </summary>
[Inject(InjectServiceLifetime.Transient)]
public class FolderGroupMediaItemViewModel() : BaseMediaItemViewModel(MediaType.FolderGroup);