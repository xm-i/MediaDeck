using MediaDeck.FileTypes.Base.Models.Interfaces;
using MediaDeck.FileTypes.Base.ViewModels;
using MediaDeck.Utils.Enums;

namespace MediaDeck.FileTypes.Pdf.ViewModels;
public class PdfFileViewModel(IFileModel fileModel) : BaseFileViewModel(fileModel) {
	public override MediaType MediaType {
		get;
	} = MediaType.Pdf;
}
