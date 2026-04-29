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

[Inject(InjectServiceLifetime.Scoped, typeof(IMediaItemFactory))]
[Inject(InjectServiceLifetime.Scoped, typeof(IMediaItemFactoryOf<VideoMediaItemViewModel>))]
public class VideoMediaItemFactory :
	BaseMediaItemFactory<VideoMediaItemOperator, VideoMediaItemModel, DefaultExecutionProgramObjectModel, VideoMediaItemViewModel, DefaultExecutionProgramConfigViewModel, VideoThumbnailPickerViewModel>,
	IMediaItemFactory<VideoMediaItemOperator, VideoMediaItemModel, DefaultExecutionProgramObjectModel, VideoMediaItemViewModel, DefaultExecutionProgramConfigViewModel, VideoThumbnailPickerViewModel>,
	IMediaItemFactoryOf<VideoMediaItemViewModel> {
	private readonly VideoMediaItemOperator _VideoMediaItemOperator;
	private readonly IServiceProvider _serviceProvider;

	public VideoMediaItemFactory(
		VideoMediaItemOperator VideoMediaItemOperator,
		ConfigModel config,
		ITagsManager tagsManager,
		IServiceProvider serviceProvider)
		: base(config, tagsManager, MediaType.Video) {
		this._VideoMediaItemOperator = VideoMediaItemOperator;
		this._serviceProvider = serviceProvider;
	}

	public override VideoMediaItemOperator CreateMediaItemOperator() {
		return this._VideoMediaItemOperator;
	}

	public override ItemType ItemType {
		get {
			return ItemType.Video;
		}
	}

	public override VideoMediaItemModel CreateMediaItemModelFromRecord(MediaItem MediaItem) {
		var ifm = this._serviceProvider.GetRequiredService<VideoMediaItemModel>();
		ifm.Initialize(MediaItem.MediaItemId, MediaItem.FilePath);
		this.SetModelProperties(ifm, MediaItem);
		return ifm;
	}

	public override VideoMediaItemViewModel CreateMediaItemViewModel(VideoMediaItemModel fileModel) {
		var vm = this._serviceProvider.GetRequiredService<VideoMediaItemViewModel>();
		vm.Initialize(fileModel);
		return vm;
	}

	public override VideoThumbnailPickerViewModel CreateThumbnailPickerViewModel() {
		return this._serviceProvider.GetRequiredService<VideoThumbnailPickerViewModel>();
	}

	public override DefaultExecutionProgramObjectModel CreateExecutionProgramObjectModel() {
		var model = this._serviceProvider.GetRequiredService<DefaultExecutionProgramObjectModel>();
		model.MediaType = this.MediaType;
		return model;
	}

	public override DefaultExecutionProgramConfigViewModel CreateExecutionProgramConfigViewModel(DefaultExecutionProgramObjectModel model) {
		var vm = this._serviceProvider.GetRequiredService<DefaultExecutionProgramConfigViewModel>();
		vm.Initialize(model);
		return vm;
	}
}