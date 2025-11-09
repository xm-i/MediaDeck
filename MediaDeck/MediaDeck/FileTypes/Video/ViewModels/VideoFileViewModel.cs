using MediaDeck.FileTypes.Base.Models.Interfaces;
using MediaDeck.FileTypes.Base.ViewModels;
using MediaDeck.Utils.Enums;

namespace MediaDeck.FileTypes.Video.ViewModels;
public class VideoFileViewModel(IFileModel fileModel) : BaseFileViewModel(fileModel) {
	public override MediaType MediaType {
		get;
	} = MediaType.Video;
}
