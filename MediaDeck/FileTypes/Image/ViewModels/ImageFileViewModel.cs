using MediaDeck.Composition.Enum;
using MediaDeck.FileTypes.Base.Models.Interfaces;
using MediaDeck.FileTypes.Base.ViewModels;

namespace MediaDeck.FileTypes.Image.ViewModels;
public class ImageFileViewModel(IFileModel fileModel) : BaseFileViewModel(fileModel) {
	public override MediaType MediaType {
		get;
	} = MediaType.Image;
}
