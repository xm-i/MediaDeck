using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.MediaItemTypes;
using MediaDeck.Composition.Interfaces.MediaItemTypes.ViewModels;
using MediaDeck.Composition.Interfaces.Tags;
using MediaDeck.Composition.Stores.Config.Model;
using MediaDeck.Composition.Tables;
using MediaDeck.MediaItemTypes.Base;
using MediaDeck.MediaItemTypes.Base.Models;
using MediaDeck.MediaItemTypes.Base.ViewModels;
using MediaDeck.MediaItemTypes.Image.Models;
using MediaDeck.MediaItemTypes.Image.ViewModels;

using Microsoft.Extensions.DependencyInjection;

namespace MediaDeck.MediaItemTypes.Image;

[Inject(InjectServiceLifetime.Scoped, typeof(IMediaItemFactory))]
[Inject(InjectServiceLifetime.Scoped, typeof(IMediaItemFactoryOf<ImageMediaItemViewModel>))]
public class ImageMediaItemFactory :
	BaseMediaItemFactory<ImageMediaItemOperator, ImageMediaItemModel, DefaultExecutionProgramObjectModel, ImageMediaItemViewModel, DefaultExecutionProgramConfigViewModel, ImageThumbnailPickerViewModel>,
	IMediaItemFactory<ImageMediaItemOperator, ImageMediaItemModel, DefaultExecutionProgramObjectModel, ImageMediaItemViewModel, DefaultExecutionProgramConfigViewModel, ImageThumbnailPickerViewModel>,
	IMediaItemFactoryOf<ImageMediaItemViewModel> {
	private readonly ImageMediaItemOperator _ImageMediaItemOperator;
	private readonly IServiceProvider _serviceProvider;

	public ImageMediaItemFactory(
		ImageMediaItemOperator ImageMediaItemOperator,
		ConfigModel config,
		ITagsManager tagsManager,
		IServiceProvider serviceProvider)
		: base(config, tagsManager, MediaType.Image) {
		this._ImageMediaItemOperator = ImageMediaItemOperator;
		this._serviceProvider = serviceProvider;
	}

	public override ImageMediaItemOperator CreateMediaItemOperator() {
		return this._ImageMediaItemOperator;
	}

	public override ImageMediaItemModel CreateMediaItemModelFromRecord(MediaItem MediaItem) {
		var ifm = this._serviceProvider.GetRequiredService<ImageMediaItemModel>();
		ifm.Initialize(MediaItem.MediaItemId, MediaItem.FilePath);
		this.SetModelProperties(ifm, MediaItem);
		return ifm;
	}

	public override ImageMediaItemViewModel CreateMediaItemViewModel(ImageMediaItemModel fileModel) {
		var vm = this._serviceProvider.GetRequiredService<ImageMediaItemViewModel>();
		vm.Initialize(fileModel);
		return vm;
	}

	public override ImageThumbnailPickerViewModel CreateThumbnailPickerViewModel() {
		return this._serviceProvider.GetRequiredService<ImageThumbnailPickerViewModel>();
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

	public override IBulkThumbnailConfigViewModel CreateBulkThumbnailConfigViewModel() {
		return this._serviceProvider.GetRequiredService<ImageBulkThumbnailConfigViewModel>();
	}
}