using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.FileTypes.Models;
using MediaDeck.FileTypes.Base.ViewModels;

namespace MediaDeck.FileTypes.Archive.ViewModels;

internal class ArchiveFileViewModel(IFileModel fileModel) : BaseFileViewModel(fileModel, MediaType.Archive);