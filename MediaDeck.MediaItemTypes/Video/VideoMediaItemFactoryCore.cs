using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.MediaItemTypes;
using MediaDeck.Composition.Interfaces.Tags;
using MediaDeck.Composition.Stores.Config.Model;
using MediaDeck.Database.Tables;
using MediaDeck.MediaItemTypes.Base;
using MediaDeck.MediaItemTypes.Base.Models;
using MediaDeck.MediaItemTypes.Base.ViewModels;
using MediaDeck.MediaItemTypes.Video.Models;
using MediaDeck.MediaItemTypes.Video.ViewModels;

using Microsoft.Extensions.DependencyInjection;

namespace MediaDeck.MediaItemTypes.Video;

public class VideoMediaItemFactoryCore : BaseMediaItemFactoryCore<VideoMediaItemOperator, VideoMediaItemModel, DefaultExecutionProgramObjectModel, VideoMediaItemViewModel, DefaultExecutionProgramConfigViewModel, VideoThumbnailPickerViewModel> {
	private readonly VideoMediaItemOperator _VideoMediaItemOperator;
	private readonly IServiceProvider _serviceProvider;
	private readonly IMediaItemTypeProvider _mediaItemTypeProvider;

	public VideoMediaItemFactoryCore(
		VideoMediaItemOperator VideoMediaItemOperator,
		ConfigModel config,
		ITagsManager tagsManager,
		IMediaItemTypeProvider mediaItemTypeProvider,
		IServiceProvider serviceProvider)
		: base(config, tagsManager, MediaType.Video) {
		this._VideoMediaItemOperator = VideoMediaItemOperator;
		this._serviceProvider = serviceProvider;
		this._mediaItemTypeProvider = mediaItemTypeProvider;
	}

	public override VideoMediaItemOperator CreateMediaItemOperator() {
		return this._VideoMediaItemOperator;
	}

	public override ItemType ItemType {
		get {
			return ItemType.Video;
		}
	}

	public override VideoMediaItemModel CreateMediaItemModelFromRecord(MediaItem MediaItem, IServiceProvider scopedServiceProvider) {
		var ifm = new VideoMediaItemModel(MediaItem.MediaItemId, MediaItem.FilePath, this._VideoMediaItemOperator, this._mediaItemTypeProvider, scopedServiceProvider);
		this.SetModelProperties(ifm, MediaItem);
		return ifm;
	}

	public override VideoMediaItemViewModel CreateMediaItemViewModel(VideoMediaItemModel fileModel) {
		return new VideoMediaItemViewModel(fileModel, this);
	}

	public override VideoThumbnailPickerViewModel CreateThumbnailPickerViewModel() {
		return this._serviceProvider.GetRequiredService<VideoThumbnailPickerViewModel>();
	}

	public override DefaultExecutionProgramObjectModel CreateExecutionProgramObjectModel() {
		return new DefaultExecutionProgramObjectModel() {
			MediaType = this.MediaType
		};
	}

	public override DefaultExecutionProgramConfigViewModel CreateExecutionProgramConfigViewModel(DefaultExecutionProgramObjectModel model) {
		return new DefaultExecutionProgramConfigViewModel(model, this._serviceProvider.GetRequiredService<IMediaItemTypeService>(), this._serviceProvider.GetRequiredService<ExecutionConfigModel>());
	}
}