using MediaDeck.Composition.Interfaces.MediaItemTypes;
using MediaDeck.Composition.Interfaces.MediaItemTypes.ViewModels;
using MediaDeck.Composition.Interfaces.MediaItemTypes.Views;
using MediaDeck.Composition.Interfaces.Tags;
using MediaDeck.Composition.Stores.Config.Model;
using MediaDeck.MediaItemTypes.Archive;
using MediaDeck.MediaItemTypes.Archive.Models;
using MediaDeck.MediaItemTypes.Archive.ViewModels;
using MediaDeck.MediaItemTypes.Base.Models;
using MediaDeck.MediaItemTypes.Base.ViewModels;
using MediaDeck.MediaItemTypes.UI.Archive.Views;
using MediaDeck.MediaItemTypes.UI.Base.Views;

namespace MediaDeck.MediaItemTypes.UI.Archive;

[Inject(InjectServiceLifetime.Singleton, typeof(IMediaItemFactory))]
public class ArchiveMediaItemFactory :
	ArchiveMediaItemFactoryCore,
	IMediaItemFactory<ArchiveMediaItemOperator, ArchiveMediaItemModel, DefaultExecutionProgramObjectModel, ArchiveMediaItemViewModel, DefaultExecutionProgramConfigViewModel, ArchiveDetailViewerPreviewControlView, ArchiveThumbnailPickerViewModel, ArchiveThumbnailPickerView, DefaultExecutionConfigView> {
	private ArchiveDetailViewerPreviewControlView? _archiveDetailViewerPreviewControlView;

	public ArchiveMediaItemFactory(
		ArchiveMediaItemOperator ArchiveMediaItemOperator,
		ConfigModel config,
		ITagsManager tagsManager,
		IMediaItemTypeProvider mediaItemTypeProvider,
		IServiceProvider serviceProvider) : base(ArchiveMediaItemOperator, config, tagsManager, mediaItemTypeProvider, serviceProvider) {
	}


	public ArchiveDetailViewerPreviewControlView CreateDetailViewerPreviewControlView(ArchiveMediaItemViewModel fileViewModel) {
		return this._archiveDetailViewerPreviewControlView ??= new ArchiveDetailViewerPreviewControlView();
	}


	public IThumbnailControlView CreateThumbnailControlView(ArchiveMediaItemViewModel fileViewModel) {
		return new ArchiveThumbnailControlView { DataContext = fileViewModel };
	}

	public ArchiveThumbnailPickerView CreateThumbnailPickerView() {
		return new ArchiveThumbnailPickerView();
	}

	public DefaultExecutionConfigView CreateExecutionConfigView(DefaultExecutionProgramConfigViewModel viewModel) {
		return new DefaultExecutionConfigView() {
			ViewModel = viewModel
		};
	}

	public IDetailViewerPreviewControlView CreateDetailViewerPreviewControlView(IMediaItemViewModel fileViewModel) {
		return this.CreateDetailViewerPreviewControlView((ArchiveMediaItemViewModel)fileViewModel);
	}

	public IThumbnailControlView CreateThumbnailControlView(IMediaItemViewModel fileViewModel) {
		return this.CreateThumbnailControlView((ArchiveMediaItemViewModel)fileViewModel);
	}

	IThumbnailPickerView IMediaItemFactory.CreateThumbnailPickerView() {
		return this.CreateThumbnailPickerView();
	}

	public IExecutionConfigView CreateExecutionConfigView(IExecutionProgramConfigViewModel viewModel) {
		return this.CreateExecutionConfigView((DefaultExecutionProgramConfigViewModel)viewModel);
	}
}