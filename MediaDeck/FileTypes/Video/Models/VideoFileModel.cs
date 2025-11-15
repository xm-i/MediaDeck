using MediaDeck.Composition.Enum;
using MediaDeck.FileTypes.Base.Models;

namespace MediaDeck.FileTypes.Video.Models;
public class VideoFileModel(long id, string filePath) : BaseFileModel(id, filePath, _fileOperator) {
	private static readonly VideoFileOperator _fileOperator = new();
	public override MediaType MediaType {
		get;
	} = MediaType.Video;
}
