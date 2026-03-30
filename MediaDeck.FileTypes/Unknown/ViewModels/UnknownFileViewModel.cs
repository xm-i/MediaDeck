using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.FileTypes.Models;
using MediaDeck.FileTypes.Base.ViewModels;

namespace MediaDeck.FileTypes.Unknown.ViewModels;

internal class UnknownFileViewModel(IFileModel fileModel) : BaseFileViewModel(fileModel, MediaType.Unknown);