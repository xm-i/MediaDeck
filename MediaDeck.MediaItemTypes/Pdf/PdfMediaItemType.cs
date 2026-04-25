using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.MediaItemTypes;
using MediaDeck.Composition.Interfaces.Tags;
using MediaDeck.Composition.Stores.Config.Model;
using MediaDeck.Database.Tables;
using MediaDeck.MediaItemTypes.Base;
using MediaDeck.MediaItemTypes.Pdf.Models;
using MediaDeck.MediaItemTypes.Pdf.ViewModels;
using MediaDeck.MediaItemTypes.Pdf.Views;
using Microsoft.Extensions.DependencyInjection;

namespace MediaDeck.MediaItemTypes.Pdf;

[Inject(InjectServiceLifetime.Singleton, typeof(IMediaItemType))]
internal class PdfMediaItemType : BaseMediaItemType<PdfMediaItemOperator, PdfMediaItemModel, PdfMediaItemViewModel, PdfDetailViewerPreviewControlView, PdfThumbnailPickerViewModel, PdfThumbnailPickerView> {
	private PdfDetailViewerPreviewControlView? _pdfDetailViewerPreviewControlView;
	private readonly PdfMediaItemOperator _PdfMediaItemOperator;
	private readonly IServiceProvider _serviceProvider;

	public PdfMediaItemType(
		PdfMediaItemOperator PdfMediaItemOperator,
		ConfigModel config,
		ITagsManager tagsManager,
		IServiceProvider serviceProvider)
		: base(config, tagsManager, MediaType.Pdf) {
		this._PdfMediaItemOperator = PdfMediaItemOperator;
		this._serviceProvider = serviceProvider;
	}

	public override PdfMediaItemOperator CreateMediaItemOperator() {
		return this._PdfMediaItemOperator;
	}

	public override PdfMediaItemModel CreateMediaItemModelFromRecord(MediaItem MediaItem) {
		var ifm = new PdfMediaItemModel(MediaItem.MediaItemId, MediaItem.FilePath, this._PdfMediaItemOperator, this._config);
		this.SetModelProperties(ifm, MediaItem);
		return ifm;
	}

	public override PdfMediaItemViewModel CreateMediaItemViewModel(PdfMediaItemModel fileModel) {
		return new PdfMediaItemViewModel(fileModel);
	}

	public override PdfDetailViewerPreviewControlView CreateDetailViewerPreviewControlView(PdfMediaItemViewModel fileViewModel) {
		return this._pdfDetailViewerPreviewControlView ??= new PdfDetailViewerPreviewControlView();
	}

	public override PdfThumbnailPickerViewModel CreateThumbnailPickerViewModel() {
		return this._serviceProvider.GetRequiredService<PdfThumbnailPickerViewModel>();
	}

	public override PdfThumbnailPickerView CreateThumbnailPickerView() {
		return new PdfThumbnailPickerView();
	}

	public override IQueryable<MediaItem> IncludeTables(IQueryable<MediaItem> MediaItems) {
		return MediaItems
			.Include(mf => mf.Container);
	}
}