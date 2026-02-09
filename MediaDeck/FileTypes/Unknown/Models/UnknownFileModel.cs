using MediaDeck.Composition.Enum;
using MediaDeck.FileTypes.Base.Models;

namespace MediaDeck.FileTypes.Unknown.Models;
public class UnknownFileModel(long id, string filePath) : BaseFileModel(id, filePath, _fileOperator) {
	private static readonly UnknownFileOperator _fileOperator = new();
	public override MediaType MediaType {
		get;
	} = MediaType.Unknown;
}
