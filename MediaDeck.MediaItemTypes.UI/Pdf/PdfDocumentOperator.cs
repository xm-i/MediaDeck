using System.Drawing.Imaging;
using System.IO;

using MediaDeck.MediaItemTypes.Pdf.Models;

using Patagames.Pdf.Enums;

using Patagames.Pdf.Net;

namespace MediaDeck.MediaItemTypes.UI.Pdf;

[Inject(InjectServiceLifetime.Transient, typeof(IPdfDocumentOperator))]
public class PdfDocumentOperator : IPdfDocumentOperator {
	/// <summary>
	/// サムネイル作成
	/// </summary>
	/// <param name="filePath">動画ファイルパス</param>
	/// <param name="width">サムネイル幅</param>
	/// <param name="height">サムネイル高さ</param>
	/// <param name="pageNumber">サムネイルにするページ番号</param>
	/// <returns>作成されたサムネイルファイル名</returns>
	public byte[] CreateThumbnail(string filePath, int width, int height, int pageNumber = 1) {
		var pdfDoc = PdfDocument.Load(filePath);
		var page = pdfDoc.Pages[pageNumber - 1];
		using var pdfBitmap = new PdfBitmap(width, height, true);
		page.Render(pdfBitmap, 0, 0, width, height, PageRotate.Normal, RenderFlags.FPDF_NONE);
		using var ms = new MemoryStream();

		using var image = pdfBitmap.GetImage();
		image.Save(ms, ImageFormat.Jpeg);
		return ms.ToArray();
	}

	public PdfProperties GetPdfProperties(string filePath) {
		var pdfDoc = PdfDocument.Load(filePath);
		var page = pdfDoc.Pages[0];
		return new PdfProperties {
			PageCount = pdfDoc.Pages.Count,
			Width = page.Width,
			Height = page.Height
		};
	}
}