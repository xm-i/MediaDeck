using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.MediaItemTypes;
using MediaDeck.Composition.Interfaces.MediaItemTypes.Views;
using MediaDeck.Composition.Interfaces.Tags;
using MediaDeck.Composition.Stores.Config.Model;
using MediaDeck.Database.Tables;
using MediaDeck.MediaItemTypes.Base.Models;
using MediaDeck.MediaItemTypes.Base.ViewModels;
using MediaDeck.MediaItemTypes.Pdf.Models;
using MediaDeck.MediaItemTypes.Pdf.ViewModels;
using MediaDeck.MediaItemTypes.UI.Base;
using MediaDeck.MediaItemTypes.UI.Base.Views;
using MediaDeck.MediaItemTypes.UI.Pdf.Views;
using Microsoft.Extensions.DependencyInjection;

namespace MediaDeck.MediaItemTypes.UI.Pdf;

[Inject(InjectServiceLifetime.Singleton, typeof(IMediaItemFactory))]
public class PdfMediaItemFactory : BaseMediaItemFactory<PdfMediaItemOperator, PdfMediaItemModel, DefaultExecutionProgramObjectModel, PdfMediaItemViewModel, DefaultExecutionProgramConfigViewModel, PdfDetailViewerPreviewControlView, PdfThumbnailPickerViewModel, PdfThumbnailPickerView, DefaultExecutionConfigView> {
	private PdfDetailViewerPreviewControlView? _pdfDetailViewerPreviewControlView;
	private readonly PdfMediaItemOperator _PdfMediaItemOperator;
	private readonly IServiceProvider _serviceProvider;

	public PdfMediaItemFactory(
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

	public override ItemType ItemType {
		get {
			return ItemType.Pdf;
		}
	}

	public override PdfMediaItemModel CreateMediaItemModelFromRecord(MediaItem MediaItem, IServiceProvider scopedServiceProvider) {
		var ifm = new PdfMediaItemModel(MediaItem.MediaItemId, MediaItem.FilePath, this._PdfMediaItemOperator, this, scopedServiceProvider);
		this.SetModelProperties(ifm, MediaItem);
		return ifm;
	}

	public override PdfMediaItemViewModel CreateMediaItemViewModel(PdfMediaItemModel fileModel) {
		return new PdfMediaItemViewModel(fileModel, this);
	}

	public override PdfDetailViewerPreviewControlView CreateDetailViewerPreviewControlView(PdfMediaItemViewModel fileViewModel) {
		return this._pdfDetailViewerPreviewControlView ??= new PdfDetailViewerPreviewControlView();
	}

	public override PdfThumbnailPickerViewModel CreateThumbnailPickerViewModel() {
		return this._serviceProvider.GetRequiredService<PdfThumbnailPickerViewModel>();
	}

	public override IThumbnailControlView CreateThumbnailControlView(PdfMediaItemViewModel fileViewModel) {
		return new PdfThumbnailControlView { DataContext = fileViewModel };
	}

	public override PdfThumbnailPickerView CreateThumbnailPickerView() {
		return new PdfThumbnailPickerView();
	}

	public override DefaultExecutionProgramObjectModel CreateExecutionProgramObjectModel() {
		return new DefaultExecutionProgramObjectModel() {
			MediaType = this.MediaType
		};
	}

	public override DefaultExecutionProgramConfigViewModel CreateExecutionProgramConfigViewModel(DefaultExecutionProgramObjectModel model) {
		return new DefaultExecutionProgramConfigViewModel(model, this._serviceProvider.GetRequiredService<IMediaItemTypeService>(), this._serviceProvider.GetRequiredService<ExecutionConfigModel>());
	}

	public override DefaultExecutionConfigView CreateExecutionConfigView(DefaultExecutionProgramConfigViewModel viewModel) {
		return new DefaultExecutionConfigView() {
			ViewModel = viewModel
		};
	}

	public override IQueryable<MediaItem> IncludeTables(IQueryable<MediaItem> MediaItems) {
		return MediaItems
			.Include(mf => mf.Container);
	}
}