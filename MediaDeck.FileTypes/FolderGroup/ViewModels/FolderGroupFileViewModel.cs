using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.FileTypes.Models;
using MediaDeck.FileTypes.Base.ViewModels;

namespace MediaDeck.FileTypes.FolderGroup.ViewModels;

internal class FolderGroupFileViewModel(IFileModel fileModel) : BaseFileViewModel(fileModel, MediaType.FolderGroup);
