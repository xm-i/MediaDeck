using MediaDeck.FileTypes.Base.Models.Interfaces;
using MediaDeck.FileTypes.Base.ViewModels;
using MediaDeck.Utils.Enums;

namespace MediaDeck.FileTypes.Image.ViewModels;
public class ImageFileViewModel(IFileModel fileModel) : BaseFileViewModel(fileModel) {
	public override MediaType MediaType {
		get;
	} = MediaType.Image;
}
