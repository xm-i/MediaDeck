using MediaDeck.Composition.Enum;
using MediaDeck.FileTypes.Base.Models;

namespace MediaDeck.FileTypes.Video.Models;

public class VideoFileModel(long id, string filePath, VideoFileOperator fileOperator) : BaseFileModel(id, filePath, fileOperator) {
	public override MediaType MediaType {
		get;
	} = MediaType.Video;
}