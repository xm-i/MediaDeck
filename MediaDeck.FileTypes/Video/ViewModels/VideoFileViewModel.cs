using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.FileTypes.Models;
using MediaDeck.FileTypes.Base.ViewModels;

namespace MediaDeck.FileTypes.Video.ViewModels;

internal class VideoFileViewModel(IFileModel fileModel) : BaseFileViewModel(fileModel, MediaType.Video);