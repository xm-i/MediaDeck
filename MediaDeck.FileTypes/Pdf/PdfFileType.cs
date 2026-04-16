using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.FileTypes;
using MediaDeck.Composition.Interfaces.Tags;
using MediaDeck.Composition.Stores.Config.Model;
using MediaDeck.Database.Tables;
using MediaDeck.FileTypes.Base;
using MediaDeck.FileTypes.Pdf.Models;
using MediaDeck.FileTypes.Pdf.ViewModels;
using MediaDeck.FileTypes.Pdf.Views;
using Microsoft.Extensions.DependencyInjection;

namespace MediaDeck.FileTypes.Pdf;

[Inject(InjectServiceLifetime.Transient, typeof(IFileType))]
internal class PdfFileType : BaseFileType<PdfFileOperator, PdfFileModel, PdfFileViewModel, PdfDetailViewerPreviewControlView, PdfThumbnailPickerViewModel, PdfThumbnailPickerView> {
	private PdfDetailViewerPreviewControlView? _pdfDetailViewerPreviewControlView;
	private readonly PdfFileOperator _pdfFileOperator;
	private readonly IServiceProvider _serviceProvider;

	public PdfFileType(
		PdfFileOperator pdfFileOperator,
		ConfigModel config,
		ITagsManager tagsManager,
		IServiceProvider serviceProvider)
		: base(config, tagsManager, MediaType.Pdf) {
		this._pdfFileOperator = pdfFileOperator;
		this._serviceProvider = serviceProvider;
	}

	public override PdfFileOperator CreateFileOperator() {
		return this._pdfFileOperator;
	}

	public override PdfFileModel CreateFileModelFromRecord(MediaFile mediaFile) {
		var ifm = new PdfFileModel(mediaFile.MediaFileId, mediaFile.FilePath, this._pdfFileOperator, this._config);
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
		return this._serviceProvider.GetRequiredService<PdfThumbnailPickerViewModel>();
	}

	public override PdfThumbnailPickerView CreateThumbnailPickerView() {
		return new PdfThumbnailPickerView();
	}

	public override IQueryable<MediaFile> IncludeTables(IQueryable<MediaFile> mediaFiles) {
		return mediaFiles
			.Include(mf => mf.Container);
	}
}