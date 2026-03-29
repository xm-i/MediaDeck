using MediaDeck.Composition.Enum;
using MediaDeck.FileTypes.Base.Models;

namespace MediaDeck.FileTypes.Pdf.Models;

public class PdfFileModel(long id, string filePath, PdfFileOperator fileOperator) : BaseFileModel(id, filePath, fileOperator) {
	public override MediaType MediaType {
		get;
	} = MediaType.Pdf;
}