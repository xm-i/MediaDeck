using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.MediaItemTypes;
using MediaDeck.Composition.Interfaces.Tags;
using MediaDeck.Composition.Stores.Config.Model;
using MediaDeck.Database.Tables;
using MediaDeck.MediaItemTypes.Base;
using MediaDeck.MediaItemTypes.Base.Models;
using MediaDeck.MediaItemTypes.Base.ViewModels;
using MediaDeck.MediaItemTypes.Unknown.Models;
using MediaDeck.MediaItemTypes.Unknown.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace MediaDeck.MediaItemTypes.Unknown;

public class UnknownMediaItemFactoryCore : BaseMediaItemFactoryCore<UnknownMediaItemOperator, UnknownMediaItemModel, DefaultExecutionProgramObjectModel, UnknownMediaItemViewModel, DefaultExecutionProgramConfigViewModel, UnknownThumbnailPickerViewModel> {
	private readonly UnknownMediaItemOperator _UnknownMediaItemOperator;
	private readonly IServiceProvider _serviceProvider;
	private readonly IMediaItemTypeProvider _mediaItemTypeProvider;

	public UnknownMediaItemFactoryCore(
		UnknownMediaItemOperator UnknownMediaItemOperator,
		ConfigModel config,
		ITagsManager tagsManager,
		IMediaItemTypeProvider mediaItemTypeProvider,
		IServiceProvider serviceProvider)
		: base(config, tagsManager, MediaType.Unknown) {
		this._UnknownMediaItemOperator = UnknownMediaItemOperator;
		this._serviceProvider = serviceProvider;
		this._mediaItemTypeProvider = mediaItemTypeProvider;
	}

	public override UnknownMediaItemOperator CreateMediaItemOperator() {
		return this._UnknownMediaItemOperator;
	}

	public override ItemType ItemType {
		get {
			return ItemType.Unknown;
		}
	}

	public override UnknownMediaItemModel CreateMediaItemModelFromRecord(MediaItem MediaItem, IServiceProvider scopedServiceProvider) {
		var ifm = new UnknownMediaItemModel(MediaItem.MediaItemId, MediaItem.FilePath, this.CreateMediaItemOperator(), this._mediaItemTypeProvider, scopedServiceProvider);
		this.SetModelProperties(ifm, MediaItem);
		return ifm;
	}

	public override UnknownMediaItemViewModel CreateMediaItemViewModel(UnknownMediaItemModel fileModel) {
		return new UnknownMediaItemViewModel(fileModel, this);
	}

	public override UnknownThumbnailPickerViewModel CreateThumbnailPickerViewModel() {
		return this._serviceProvider.GetRequiredService<UnknownThumbnailPickerViewModel>();
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