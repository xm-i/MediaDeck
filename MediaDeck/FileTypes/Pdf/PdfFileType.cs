using CommunityToolkit.Mvvm.DependencyInjection;

using MediaDeck.Database.Tables;
using MediaDeck.FileTypes.Base;
using MediaDeck.FileTypes.Pdf.Models;
using MediaDeck.FileTypes.Pdf.ViewModels;
using MediaDeck.FileTypes.Pdf.Views;
using MediaDeck.Utils.Enums;

namespace MediaDeck.FileTypes.Pdf;
[AddTransient(typeof(IFileType))]
public class PdfFileType: BaseFileType<PdfFileOperator, PdfFileModel, PdfFileViewModel, PdfDetailViewerPreviewControlView, PdfThumbnailPickerViewModel, PdfThumbnailPickerView> {
	private PdfDetailViewerPreviewControlView? _pdfDetailViewerPreviewControlView;
	public override MediaType MediaType {
		get;
	} = MediaType.Pdf;

	public override PdfFileOperator CreateFileOperator() {
		return new PdfFileOperator();
	}

	public override PdfFileModel CreateFileModelFromRecord(MediaFile mediaFile) {
		var ifm = new PdfFileModel(mediaFile.MediaFileId, mediaFile.FilePath);
		this.SetModelProperties(ifm, mediaFile);
		return ifm;
	}

	public override PdfFileViewModel CreateFileViewModel(PdfFileModel fileModel) {
		return new PdfFileViewModel(fileModel);
	}
	public override PdfDetailViewerPreviewControlView CreateDetailViewerPreviewControlView(PdfFileViewModel fileViewModel) {
		return this._pdfDetailViewerPreviewControlView ??= new PdfDetailViewerPreviewControlView();
	}

	public override PdfThumbnailPickerViewModel CreateThumbnailPickerViewModel() {
		return Ioc.Default.GetRequiredService<PdfThumbnailPickerViewModel>();
	}

	public override PdfThumbnailPickerView CreateThumbnailPickerView() {
		return new PdfThumbnailPickerView();
	}
	public override IQueryable<MediaFile> IncludeTables(IQueryable<MediaFile> mediaFiles) {
		return mediaFiles
			.Include(mf => mf.Container);
	}
}
