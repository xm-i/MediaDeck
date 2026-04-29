using MediaDeck.Composition.Interfaces.MediaItemTypes;
using MediaDeck.Composition.Interfaces.MediaItemTypes.ViewModels;
using MediaDeck.Composition.Interfaces.MediaItemTypes.Views;
using MediaDeck.Composition.Interfaces.Tags;
using MediaDeck.Composition.Stores.Config.Model;
using MediaDeck.MediaItemTypes.Base.Models;
using MediaDeck.MediaItemTypes.Base.ViewModels;
using MediaDeck.MediaItemTypes.Pdf;
using MediaDeck.MediaItemTypes.Pdf.Models;
using MediaDeck.MediaItemTypes.Pdf.ViewModels;
using MediaDeck.MediaItemTypes.UI.Base.Views;
using MediaDeck.MediaItemTypes.UI.Pdf.Views;

namespace MediaDeck.MediaItemTypes.UI.Pdf;

[Inject(InjectServiceLifetime.Scoped, typeof(IMediaItemFactory))]
[Inject(InjectServiceLifetime.Scoped, typeof(IMediaItemFactoryOf<PdfMediaItemViewModel>))]
public class PdfMediaItemFactory : PdfMediaItemFactoryCore,
	IMediaItemFactory<PdfMediaItemOperator, PdfMediaItemModel, DefaultExecutionProgramObjectModel, PdfMediaItemViewModel, DefaultExecutionProgramConfigViewModel, PdfDetailViewerPreviewControlView, PdfThumbnailPickerViewModel, PdfThumbnailPickerView, DefaultExecutionConfigView>,
	IMediaItemFactoryOf<PdfMediaItemViewModel> {
	private PdfDetailViewerPreviewControlView? _pdfDetailViewerPreviewControlView;

	public PdfMediaItemFactory(
		PdfMediaItemOperator PdfMediaItemOperator,
		ConfigModel config,
		ITagsManager tagsManager,
		IServiceProvider serviceProvider)
		: base(PdfMediaItemOperator, config, tagsManager, serviceProvider) {
	}

	public PdfDetailViewerPreviewControlView CreateDetailViewerPreviewControlView(PdfMediaItemViewModel fileViewModel) {
		return this._pdfDetailViewerPreviewControlView ??= new PdfDetailViewerPreviewControlView();
	}


	public IThumbnailControlView CreateThumbnailControlView(PdfMediaItemViewModel fileViewModel) {
		return new PdfThumbnailControlView { DataContext = fileViewModel };
	}

	public PdfThumbnailPickerView CreateThumbnailPickerView() {
		return new PdfThumbnailPickerView();
	}

	public DefaultExecutionConfigView CreateExecutionConfigView(DefaultExecutionProgramConfigViewModel viewModel) {
		return new DefaultExecutionConfigView() {
			ViewModel = viewModel
		};
	}

	public IDetailViewerPreviewControlView CreateDetailViewerPreviewControlView(IMediaItemViewModel fileViewModel) {
		return this.CreateDetailViewerPreviewControlView((PdfMediaItemViewModel)fileViewModel);
	}

	public IThumbnailControlView CreateThumbnailControlView(IMediaItemViewModel fileViewModel) {
		return this.CreateThumbnailControlView((PdfMediaItemViewModel)fileViewModel);
	}

	IThumbnailPickerView IMediaItemFactory.CreateThumbnailPickerView() {
		return this.CreateThumbnailPickerView();
	}

	public IExecutionConfigView CreateExecutionConfigView(IExecutionProgramConfigViewModel viewModel) {
		return this.CreateExecutionConfigView((DefaultExecutionProgramConfigViewModel)viewModel);
	}
}