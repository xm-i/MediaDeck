using MediaDeck.Composition.Enum;
using MediaDeck.FileTypes.Base.Models;

namespace MediaDeck.FileTypes.Archive.Models;
public class ArchiveFileModel(long id, string filePath, ArchiveFileOperator fileOperator) : BaseFileModel(id, filePath, fileOperator) {
	public override MediaType MediaType {
		get;
	} = MediaType.Archive;
}
