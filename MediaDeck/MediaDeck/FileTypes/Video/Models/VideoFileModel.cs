using MediaDeck.FileTypes.Base.Models;
using MediaDeck.Utils.Enums;

namespace MediaDeck.FileTypes.Video.Models;
public class VideoFileModel(long id, string filePath) : BaseFileModel(id, filePath, _fileOperator) {
	private static readonly VideoFileOperator _fileOperator = new();
	public override MediaType MediaType {
		get;
	} = MediaType.Video;
}
