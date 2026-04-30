using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.MediaItemTypes.ViewModels;
using MediaDeck.MediaItemTypes.Base.Models;
using MediaDeck.MediaItemTypes.Base.ViewModels;
using MediaDeck.MediaItemTypes.Pdf.Models;

namespace MediaDeck.MediaItemTypes.Pdf.ViewModels;

/// <summary>
/// PDF 用の一括サムネイル再生成設定ViewModel。抽出するページ番号を指定可能。
/// </summary>
[Inject(InjectServiceLifetime.Transient)]
public class PdfBulkThumbnailConfigViewModel : BulkThumbnailConfigViewModelBase {
	private readonly IPdfDocumentOperator _pdfDocumentOperator;

	public PdfBulkThumbnailConfigViewModel(IPdfDocumentOperator pdfDocumentOperator, BaseThumbnailPickerModel thumbnailPickerModel)
		: base(MediaType.Pdf, thumbnailPickerModel) {
		this._pdfDocumentOperator = pdfDocumentOperator;
	}

	/// <summary>
	/// 対象ページ番号 (1 始まり)。
	/// </summary>
	public BindableReactiveProperty<int> PageNumber {
		get;
	} = new(1);

	protected override byte[]? GenerateThumbnail(IMediaItemViewModel target) {
		var page = Math.Max(1, this.PageNumber.Value);
		return this._pdfDocumentOperator.CreateThumbnail(target.FilePath, 300, 300, page);
	}
}