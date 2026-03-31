using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.FileTypes.Models;
using MediaDeck.FileTypes.Base.ViewModels;

namespace MediaDeck.FileTypes.Image.ViewModels;

internal class ImageFileViewModel(IFileModel fileModel) : BaseFileViewModel(fileModel, MediaType.Image) { }