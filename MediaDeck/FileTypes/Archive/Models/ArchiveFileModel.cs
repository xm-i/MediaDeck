using MediaDeck.FileTypes.Base.Models;
using MediaDeck.Utils.Enums;

namespace MediaDeck.FileTypes.Archive.Models;
public class ArchiveFileModel(long id, string filePath) : BaseFileModel(id, filePath, _fileOperator) {
	private static readonly ArchiveFileOperator _fileOperator = new();
	public override MediaType MediaType {
		get;
	} = MediaType.Archive;
}
