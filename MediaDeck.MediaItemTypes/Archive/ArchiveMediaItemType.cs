using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.MediaItemTypes;
using MediaDeck.Composition.Interfaces.Tags;
using MediaDeck.Composition.Stores.Config.Model;
using MediaDeck.Database.Tables;
using MediaDeck.MediaItemTypes.Archive.Models;
using MediaDeck.MediaItemTypes.Archive.ViewModels;
using MediaDeck.MediaItemTypes.Archive.Views;
using MediaDeck.MediaItemTypes.Base;
using Microsoft.Extensions.DependencyInjection;

namespace MediaDeck.MediaItemTypes.Archive;

[Inject(InjectServiceLifetime.Singleton, typeof(IMediaItemType))]
internal class ArchiveMediaItemType : BaseMediaItemType<ArchiveMediaItemOperator, ArchiveMediaItemModel, ArchiveMediaItemViewModel, ArchiveDetailViewerPreviewControlView, ArchiveThumbnailPickerViewModel, ArchiveThumbnailPickerView> {
	private ArchiveDetailViewerPreviewControlView? _archiveDetailViewerPreviewControlView;
	private readonly ArchiveMediaItemOperator _ArchiveMediaItemOperator;
	private readonly IServiceProvider _serviceProvider;

	public ArchiveMediaItemType(
		ArchiveMediaItemOperator ArchiveMediaItemOperator,
		ConfigModel config,
		ITagsManager tagsManager,
		IServiceProvider serviceProvider)
		: base(config, tagsManager, MediaType.Archive) {
		this._ArchiveMediaItemOperator = ArchiveMediaItemOperator;
		this._serviceProvider = serviceProvider;
	}

	public override ArchiveMediaItemOperator CreateMediaItemOperator() {
		return this._ArchiveMediaItemOperator;
	}

	public override ArchiveMediaItemModel CreateMediaItemModelFromRecord(MediaItem MediaItem) {
		var ifm = new ArchiveMediaItemModel(MediaItem.MediaItemId, MediaItem.FilePath, this._ArchiveMediaItemOperator, this._config);
		this.SetModelProperties(ifm, MediaItem);
		return ifm;
	}

	public override ArchiveMediaItemViewModel CreateMediaItemViewModel(ArchiveMediaItemModel fileModel) {
		return new ArchiveMediaItemViewModel(fileModel);
	}

	public override ArchiveDetailViewerPreviewControlView CreateDetailViewerPreviewControlView(ArchiveMediaItemViewModel fileViewModel) {
		return this._archiveDetailViewerPreviewControlView ??= new ArchiveDetailViewerPreviewControlView();
	}

	public override ArchiveThumbnailPickerViewModel CreateThumbnailPickerViewModel() {
		return this._serviceProvider.GetRequiredService<ArchiveThumbnailPickerViewModel>();
	}

	public override ArchiveThumbnailPickerView CreateThumbnailPickerView() {
		return new ArchiveThumbnailPickerView();
	}

	public override IQueryable<MediaItem> IncludeTables(IQueryable<MediaItem> MediaItems) {
		return MediaItems
			.Include(mf => mf.Container);
	}
}