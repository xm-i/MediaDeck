using MediaDeck.Composition.Enum;
using MediaDeck.FileTypes.Base.Models;

namespace MediaDeck.FileTypes.Pdf.Models;
public class PdfFileModel(long id, string filePath) : BaseFileModel(id, filePath, _fileOperator) {
	private static readonly PdfFileOperator _fileOperator = new();
	public override MediaType MediaType {
		get;
	} = MediaType.Pdf;
}
