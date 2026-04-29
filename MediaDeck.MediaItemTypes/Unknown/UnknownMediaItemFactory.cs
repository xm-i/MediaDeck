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

[Inject(InjectServiceLifetime.Scoped, typeof(IMediaItemFactory))]
[Inject(InjectServiceLifetime.Scoped, typeof(IMediaItemFactoryOf<UnknownMediaItemViewModel>))]
public class UnknownMediaItemFactory :
	BaseMediaItemFactory<UnknownMediaItemOperator, UnknownMediaItemModel, DefaultExecutionProgramObjectModel, UnknownMediaItemViewModel, DefaultExecutionProgramConfigViewModel, UnknownThumbnailPickerViewModel>,
	IMediaItemFactory<UnknownMediaItemOperator, UnknownMediaItemModel, DefaultExecutionProgramObjectModel, UnknownMediaItemViewModel, DefaultExecutionProgramConfigViewModel, UnknownThumbnailPickerViewModel>,
	IMediaItemFactoryOf<UnknownMediaItemViewModel> {
	private readonly UnknownMediaItemOperator _UnknownMediaItemOperator;
	private readonly IServiceProvider _serviceProvider;

	public UnknownMediaItemFactory(
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

	public override ItemType ItemType {
		get {
			return ItemType.Unknown;
		}
	}

	public override UnknownMediaItemModel CreateMediaItemModelFromRecord(MediaItem MediaItem) {
		var ifm = this._serviceProvider.GetRequiredService<UnknownMediaItemModel>();
		ifm.Initialize(MediaItem.MediaItemId, MediaItem.FilePath);
		this.SetModelProperties(ifm, MediaItem);
		return ifm;
	}

	public override UnknownMediaItemViewModel CreateMediaItemViewModel(UnknownMediaItemModel fileModel) {
		var vm = this._serviceProvider.GetRequiredService<UnknownMediaItemViewModel>();
		vm.Initialize(fileModel);
		return vm;
	}

	public override UnknownThumbnailPickerViewModel CreateThumbnailPickerViewModel() {
		return this._serviceProvider.GetRequiredService<UnknownThumbnailPickerViewModel>();
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