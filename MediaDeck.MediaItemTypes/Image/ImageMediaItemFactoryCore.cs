using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.MediaItemTypes;
using MediaDeck.Composition.Interfaces.Tags;
using MediaDeck.Composition.Stores.Config.Model;
using MediaDeck.Database.Tables;
using MediaDeck.MediaItemTypes.Base;
using MediaDeck.MediaItemTypes.Base.Models;
using MediaDeck.MediaItemTypes.Base.ViewModels;
using MediaDeck.MediaItemTypes.Image.Models;
using MediaDeck.MediaItemTypes.Image.ViewModels;

using Microsoft.Extensions.DependencyInjection;

namespace MediaDeck.MediaItemTypes.Image;

public class ImageMediaItemFactoryCore :
	BaseMediaItemFactoryCore<ImageMediaItemOperator, ImageMediaItemModel, DefaultExecutionProgramObjectModel, ImageMediaItemViewModel, DefaultExecutionProgramConfigViewModel, ImageThumbnailPickerViewModel> {
	private readonly ImageMediaItemOperator _ImageMediaItemOperator;
	private readonly IServiceProvider _serviceProvider;
	private readonly IMediaItemTypeProvider _mediaItemTypeProvider;

	public ImageMediaItemFactoryCore(
		ImageMediaItemOperator ImageMediaItemOperator,
		ConfigModel config,
		ITagsManager tagsManager,
		IMediaItemTypeProvider mediaItemTypeProvider,
		IServiceProvider serviceProvider)
		: base(config, tagsManager, MediaType.Image) {
		this._ImageMediaItemOperator = ImageMediaItemOperator;
		this._serviceProvider = serviceProvider;
		this._mediaItemTypeProvider = mediaItemTypeProvider;
	}

	public override ImageMediaItemOperator CreateMediaItemOperator() {
		return this._ImageMediaItemOperator;
	}

	public override ItemType ItemType {
		get {
			return ItemType.Image;
		}
	}

	public override ImageMediaItemModel CreateMediaItemModelFromRecord(MediaItem MediaItem, IServiceProvider scopedServiceProvider) {
		var ifm = new ImageMediaItemModel(MediaItem.MediaItemId, MediaItem.FilePath, this._ImageMediaItemOperator, this._mediaItemTypeProvider, scopedServiceProvider);
		this.SetModelProperties(ifm, MediaItem);
		return ifm;
	}

	public override ImageMediaItemViewModel CreateMediaItemViewModel(ImageMediaItemModel fileModel) {
		return new ImageMediaItemViewModel(fileModel, this);
	}

	public override ImageThumbnailPickerViewModel CreateThumbnailPickerViewModel() {
		return this._serviceProvider.GetRequiredService<ImageThumbnailPickerViewModel>();
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