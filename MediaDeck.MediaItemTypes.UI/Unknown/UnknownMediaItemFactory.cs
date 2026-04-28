using MediaDeck.Composition.Interfaces.MediaItemTypes;
using MediaDeck.Composition.Interfaces.MediaItemTypes.ViewModels;
using MediaDeck.Composition.Interfaces.MediaItemTypes.Views;
using MediaDeck.Composition.Interfaces.Tags;
using MediaDeck.Composition.Stores.Config.Model;
using MediaDeck.MediaItemTypes.Base.Models;
using MediaDeck.MediaItemTypes.Base.ViewModels;
using MediaDeck.MediaItemTypes.UI.Base.Views;
using MediaDeck.MediaItemTypes.UI.Unknown.Views;
using MediaDeck.MediaItemTypes.Unknown;
using MediaDeck.MediaItemTypes.Unknown.Models;
using MediaDeck.MediaItemTypes.Unknown.ViewModels;

namespace MediaDeck.MediaItemTypes.UI.Unknown;

[Inject(InjectServiceLifetime.Singleton, typeof(IMediaItemFactory))]
public class UnknownMediaItemFactory : UnknownMediaItemFactoryCore,
	IMediaItemFactory<UnknownMediaItemOperator, UnknownMediaItemModel, DefaultExecutionProgramObjectModel, UnknownMediaItemViewModel, DefaultExecutionProgramConfigViewModel, UnknownDetailViewerPreviewControlView, UnknownThumbnailPickerViewModel, UnknownThumbnailPickerView, DefaultExecutionConfigView> {
	private UnknownDetailViewerPreviewControlView? _unknownDetailViewerPreviewControlView;

	public UnknownMediaItemFactory(
		UnknownMediaItemOperator UnknownMediaItemOperator,
		ConfigModel config,
		ITagsManager tagsManager,
		IMediaItemTypeProvider mediaItemTypeProvider,
		IServiceProvider serviceProvider)
		: base(UnknownMediaItemOperator, config, tagsManager, mediaItemTypeProvider, serviceProvider) {
	}

	public UnknownDetailViewerPreviewControlView CreateDetailViewerPreviewControlView(UnknownMediaItemViewModel fileViewModel) {
		return this._unknownDetailViewerPreviewControlView ??= new UnknownDetailViewerPreviewControlView();
	}

	public IThumbnailControlView CreateThumbnailControlView(UnknownMediaItemViewModel fileViewModel) {
		return new UnknownThumbnailControlView { DataContext = fileViewModel };
	}

	public UnknownThumbnailPickerView CreateThumbnailPickerView() {
		return new UnknownThumbnailPickerView();
	}


	public DefaultExecutionConfigView CreateExecutionConfigView(DefaultExecutionProgramConfigViewModel viewModel) {
		return new DefaultExecutionConfigView() {
			ViewModel = viewModel
		};
	}

	public IDetailViewerPreviewControlView CreateDetailViewerPreviewControlView(IMediaItemViewModel fileViewModel) {
		return this.CreateDetailViewerPreviewControlView((UnknownMediaItemViewModel)fileViewModel);
	}

	public IThumbnailControlView CreateThumbnailControlView(IMediaItemViewModel fileViewModel) {
		return this.CreateThumbnailControlView((UnknownMediaItemViewModel)fileViewModel);
	}

	IThumbnailPickerView IMediaItemFactory.CreateThumbnailPickerView() {
		return this.CreateThumbnailPickerView();
	}

	public IExecutionConfigView CreateExecutionConfigView(IExecutionProgramConfigViewModel viewModel) {
		return this.CreateExecutionConfigView((DefaultExecutionProgramConfigViewModel)viewModel);
	}
}