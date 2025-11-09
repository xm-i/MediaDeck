using MediaDeck.FileTypes.Base.Models.Interfaces;
using MediaDeck.FileTypes.Base.ViewModels;
using MediaDeck.Utils.Enums;

namespace MediaDeck.FileTypes.Archive.ViewModels;
public class ArchiveFileViewModel(IFileModel fileModel) : BaseFileViewModel(fileModel) {
	public override MediaType MediaType {
		get;
	} = MediaType.Archive;
}
