using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.FileTypes.Models;
using MediaDeck.FileTypes.Base.ViewModels;

namespace MediaDeck.FileTypes.Pdf.ViewModels;
public class PdfFileViewModel(IFileModel fileModel) : BaseFileViewModel(fileModel) {
	public override MediaType MediaType {
		get;
	} = MediaType.Pdf;
}
