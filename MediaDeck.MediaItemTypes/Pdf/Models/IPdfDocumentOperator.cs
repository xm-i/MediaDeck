namespace MediaDeck.MediaItemTypes.Pdf.Models;

public interface IPdfDocumentOperator {
	/// <summary>
	/// サムネイル作成
	/// </summary>
	/// <param name="filePath">動画ファイルパス</param>
	/// <param name="width">サムネイル幅</param>
	/// <param name="height">サムネイル高さ</param>
	/// <param name="pageNumber">サムネイルにするページ番号</param>
	/// <returns>作成されたサムネイルファイル名</returns>
	public byte[] CreateThumbnail(string filePath, int width, int height, int pageNumber = 1);

	public PdfProperties GetPdfProperties(string filePath);
}

public class PdfProperties {
	public int PageCount {
		get; set;
	}
	public double Width {
		get; set;
	}
	public double Height {
		get; set;
	}
}