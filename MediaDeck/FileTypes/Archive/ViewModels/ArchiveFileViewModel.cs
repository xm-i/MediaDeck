using MediaDeck.Composition.Enum;
using MediaDeck.FileTypes.Base.Models.Interfaces;
using MediaDeck.FileTypes.Base.ViewModels;

namespace MediaDeck.FileTypes.Archive.ViewModels;
public class ArchiveFileViewModel(IFileModel fileModel) : BaseFileViewModel(fileModel) {
	public override MediaType MediaType {
		get;
	} = MediaType.Archive;
}
