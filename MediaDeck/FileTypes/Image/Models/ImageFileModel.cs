using MediaDeck.Composition.Enum;
using MediaDeck.FileTypes.Base.Models;

namespace MediaDeck.FileTypes.Image.Models;
public class ImageFileModel(long id, string filePath) : BaseFileModel(id, filePath, _fileOperator) {
	private static readonly ImageFileOperator _fileOperator = new();
	public override MediaType MediaType {
		get;
	} = MediaType.Image;
}
