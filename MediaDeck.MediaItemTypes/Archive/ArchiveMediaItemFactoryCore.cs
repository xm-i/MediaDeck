using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.MediaItemTypes;
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

public class ArchiveMediaItemFactoryCore : BaseMediaItemFactoryCore<ArchiveMediaItemOperator, ArchiveMediaItemModel, DefaultExecutionProgramObjectModel, ArchiveMediaItemViewModel, DefaultExecutionProgramConfigViewModel, ArchiveThumbnailPickerViewModel> {
	private readonly ArchiveMediaItemOperator _ArchiveMediaItemOperator;
	private readonly IServiceProvider _serviceProvider;
	private readonly IMediaItemTypeProvider _mediaItemTypeProvider;

	public ArchiveMediaItemFactoryCore(
		ArchiveMediaItemOperator ArchiveMediaItemOperator,
		ConfigModel config,
		ITagsManager tagsManager,
		IMediaItemTypeProvider mediaItemTypeProvider,
		IServiceProvider serviceProvider)
		: base(config, tagsManager, MediaType.Archive) {
		this._ArchiveMediaItemOperator = ArchiveMediaItemOperator;
		this._serviceProvider = serviceProvider;
		this._mediaItemTypeProvider = mediaItemTypeProvider;
	}

	public override ArchiveMediaItemOperator CreateMediaItemOperator() {
		return this._ArchiveMediaItemOperator;
	}

	public override ItemType ItemType {
		get {
			return ItemType.Archive;
		}
	}

	public override ArchiveMediaItemModel CreateMediaItemModelFromRecord(MediaItem MediaItem, IServiceProvider scopedServiceProvider) {
		var ifm = new ArchiveMediaItemModel(MediaItem.MediaItemId, MediaItem.FilePath, this._ArchiveMediaItemOperator, this._mediaItemTypeProvider, scopedServiceProvider);
		this.SetModelProperties(ifm, MediaItem);
		return ifm;
	}

	public override ArchiveMediaItemViewModel CreateMediaItemViewModel(ArchiveMediaItemModel fileModel) {
		return new ArchiveMediaItemViewModel(fileModel, this);
	}

	public override ArchiveThumbnailPickerViewModel CreateThumbnailPickerViewModel() {
		return this._serviceProvider.GetRequiredService<ArchiveThumbnailPickerViewModel>();
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