using MediaDeck.Composition.Interfaces.MediaItemTypes;
using MediaDeck.Composition.Interfaces.MediaItemTypes.ViewModels;
using MediaDeck.Composition.Interfaces.MediaItemTypes.Views;
using MediaDeck.Composition.Interfaces.Tags;
using MediaDeck.Composition.Stores.Config.Model;
using MediaDeck.MediaItemTypes.FolderGroup;
using MediaDeck.MediaItemTypes.FolderGroup.Models;
using MediaDeck.MediaItemTypes.FolderGroup.ViewModels;
using MediaDeck.MediaItemTypes.UI.FolderGroup.Views;

namespace MediaDeck.MediaItemTypes.UI.FolderGroup;

[Inject(InjectServiceLifetime.Scoped, typeof(IMediaItemFactory))]
[Inject(InjectServiceLifetime.Scoped, typeof(IMediaItemFactoryOf<FolderGroupMediaItemViewModel>))]
public class FolderGroupMediaItemFactory :
	FolderGroupMediaItemFactoryCore,
	IMediaItemFactory<FolderGroupMediaItemOperator, FolderGroupMediaItemModel, FolderGroupExecutionProgramObjectModel, FolderGroupMediaItemViewModel, FolderGroupExecutionProgramConfigViewModel, FolderGroupDetailViewerPreviewControlView, FolderGroupThumbnailPickerViewModel, FolderGroupThumbnailPickerView, FolderGroupExecutionConfigView>,
	IMediaItemFactoryOf<FolderGroupMediaItemViewModel> {
	private FolderGroupDetailViewerPreviewControlView? _detailViewerPreviewControlView;

	public FolderGroupMediaItemFactory(
		FolderGroupMediaItemOperator fileOperator,
		ConfigModel config,
		ITagsManager tagsManager,
		IServiceProvider serviceProvider)
		: base(fileOperator, config, tagsManager, serviceProvider) {
	}

	/// <inheritdoc />
	public FolderGroupExecutionConfigView CreateExecutionConfigView(FolderGroupExecutionProgramConfigViewModel viewModel) {
		return new FolderGroupExecutionConfigView {
			ViewModel = viewModel
		};
	}

	public FolderGroupDetailViewerPreviewControlView CreateDetailViewerPreviewControlView(FolderGroupMediaItemViewModel fileViewModel) {
		return this._detailViewerPreviewControlView ??= new FolderGroupDetailViewerPreviewControlView();
	}

	public IThumbnailControlView CreateThumbnailControlView(FolderGroupMediaItemViewModel fileViewModel) {
		return new FolderGroupThumbnailControlView { DataContext = fileViewModel };
	}

	public FolderGroupThumbnailPickerView CreateThumbnailPickerView() {
		return new FolderGroupThumbnailPickerView();
	}

	public IDetailViewerPreviewControlView CreateDetailViewerPreviewControlView(IMediaItemViewModel fileViewModel) {
		return this.CreateDetailViewerPreviewControlView((FolderGroupMediaItemViewModel)fileViewModel);
	}

	public IThumbnailControlView CreateThumbnailControlView(IMediaItemViewModel fileViewModel) {
		return this.CreateThumbnailControlView((FolderGroupMediaItemViewModel)fileViewModel);
	}

	IThumbnailPickerView IMediaItemFactory.CreateThumbnailPickerView() {
		return this.CreateThumbnailPickerView();
	}

	public IExecutionConfigView CreateExecutionConfigView(IExecutionProgramConfigViewModel viewModel) {
		return this.CreateExecutionConfigView((FolderGroupExecutionProgramConfigViewModel)viewModel);
	}

}