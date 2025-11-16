using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.FileTypes.Models;
using MediaDeck.FileTypes.Base.ViewModels;

namespace MediaDeck.FileTypes.Image.ViewModels;
public class ImageFileViewModel(IFileModel fileModel) : BaseFileViewModel(fileModel) {
	public override MediaType MediaType {
		get;
	} = MediaType.Image;
}
