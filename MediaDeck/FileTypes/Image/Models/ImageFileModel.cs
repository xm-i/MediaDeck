using MediaDeck.Composition.Enum;
using MediaDeck.FileTypes.Base.Models;

namespace MediaDeck.FileTypes.Image.Models;
public class ImageFileModel(long id, string filePath, ImageFileOperator fileOperator) : BaseFileModel(id, filePath, fileOperator) {
	public override MediaType MediaType {
		get;
	} = MediaType.Image;
}
