using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.MediaItemTypes;
using MediaDeck.Composition.Interfaces.MediaItemTypes.ViewModels;
using MediaDeck.Composition.Interfaces.Tags;
using MediaDeck.Composition.Stores.Config.Model;
using MediaDeck.Database.Tables;
using MediaDeck.MediaItemTypes.Archive.Models;
using MediaDeck.MediaItemTypes.Archive.ViewModels;
using MediaDeck.MediaItemTypes.Base;
using MediaDeck.MediaItemTypes.Base.Models;
using MediaDeck.MediaItemTypes.Base.ViewModels;

using Microsoft.Extensions.DependencyInjection;

namespace MediaDeck.MediaItemTypes.Archive;

[Inject(InjectServiceLifetime.Scoped, typeof(IMediaItemFactory))]
[Inject(InjectServiceLifetime.Scoped, typeof(IMediaItemFactoryOf<ArchiveMediaItemViewModel>))]
public class ArchiveMediaItemFactory :
	BaseMediaItemFactory<ArchiveMediaItemOperator, ArchiveMediaItemModel, DefaultExecutionProgramObjectModel, ArchiveMediaItemViewModel, DefaultExecutionProgramConfigViewModel, ArchiveThumbnailPickerViewModel>,
	IMediaItemFactory<ArchiveMediaItemOperator, ArchiveMediaItemModel, DefaultExecutionProgramObjectModel, ArchiveMediaItemViewModel, DefaultExecutionProgramConfigViewModel, ArchiveThumbnailPickerViewModel>,
	IMediaItemFactoryOf<ArchiveMediaItemViewModel> {
	private readonly ArchiveMediaItemOperator _ArchiveMediaItemOperator;
	private readonly IServiceProvider _serviceProvider;

	public ArchiveMediaItemFactory(
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

	public override ItemType ItemType {
		get {
			return ItemType.Archive;
		}
	}

	public override ArchiveMediaItemModel CreateMediaItemModelFromRecord(MediaItem MediaItem) {
		var ifm = this._serviceProvider.GetRequiredService<ArchiveMediaItemModel>();
		ifm.Initialize(MediaItem.MediaItemId, MediaItem.FilePath);
		this.SetModelProperties(ifm, MediaItem);
		return ifm;
	}

	public override ArchiveMediaItemViewModel CreateMediaItemViewModel(ArchiveMediaItemModel fileModel) {
		var vm = this._serviceProvider.GetRequiredService<ArchiveMediaItemViewModel>();
		vm.Initialize(fileModel);
		return vm;
	}

	public override ArchiveThumbnailPickerViewModel CreateThumbnailPickerViewModel() {
		return this._serviceProvider.GetRequiredService<ArchiveThumbnailPickerViewModel>();
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
		return this._serviceProvider.GetRequiredService<ArchiveBulkThumbnailConfigViewModel>();
	}
}