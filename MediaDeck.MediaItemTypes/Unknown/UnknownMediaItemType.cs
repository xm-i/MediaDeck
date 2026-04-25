using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.MediaItemTypes;
using MediaDeck.Composition.Interfaces.Tags;
using MediaDeck.Composition.Stores.Config.Model;
using MediaDeck.Database.Tables;
using MediaDeck.MediaItemTypes.Base;
using MediaDeck.MediaItemTypes.Unknown.Models;
using MediaDeck.MediaItemTypes.Unknown.ViewModels;
using MediaDeck.MediaItemTypes.Unknown.Views;
using Microsoft.Extensions.DependencyInjection;

namespace MediaDeck.MediaItemTypes.Unknown;

[Inject(InjectServiceLifetime.Singleton, typeof(IMediaItemType))]
internal class UnknownMediaItemType : BaseMediaItemType<UnknownMediaItemOperator, UnknownMediaItemModel, UnknownMediaItemViewModel, UnknownDetailViewerPreviewControlView, UnknownThumbnailPickerViewModel, UnknownThumbnailPickerView> {
	private UnknownDetailViewerPreviewControlView? _unknownDetailViewerPreviewControlView;
	private readonly UnknownMediaItemOperator _UnknownMediaItemOperator;
	private readonly IServiceProvider _serviceProvider;

	public UnknownMediaItemType(
		UnknownMediaItemOperator UnknownMediaItemOperator,
		ConfigModel config,
		ITagsManager tagsManager,
		IServiceProvider serviceProvider)
		: base(config, tagsManager, MediaType.Unknown) {
		this._UnknownMediaItemOperator = UnknownMediaItemOperator;
		this._serviceProvider = serviceProvider;
	}

	public override UnknownMediaItemOperator CreateMediaItemOperator() {
		return this._UnknownMediaItemOperator;
	}

	public override UnknownMediaItemModel CreateMediaItemModelFromRecord(MediaItem MediaItem) {
		var ifm = new UnknownMediaItemModel(MediaItem.MediaItemId, MediaItem.FilePath, this.CreateMediaItemOperator(), this._config);
		this.SetModelProperties(ifm, MediaItem);
		return ifm;
	}

	public override UnknownMediaItemViewModel CreateMediaItemViewModel(UnknownMediaItemModel fileModel) {
		return new UnknownMediaItemViewModel(fileModel);
	}

	public override UnknownDetailViewerPreviewControlView CreateDetailViewerPreviewControlView(UnknownMediaItemViewModel fileViewModel) {
		return this._unknownDetailViewerPreviewControlView ??= new UnknownDetailViewerPreviewControlView();
	}

	public override UnknownThumbnailPickerViewModel CreateThumbnailPickerViewModel() {
		return this._serviceProvider.GetRequiredService<UnknownThumbnailPickerViewModel>();
	}

	public override UnknownThumbnailPickerView CreateThumbnailPickerView() {
		return new UnknownThumbnailPickerView();
	}

	public override IQueryable<MediaItem> IncludeTables(IQueryable<MediaItem> MediaItems) {
		return MediaItems;
	}
}