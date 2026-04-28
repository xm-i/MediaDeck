using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.MediaItemTypes;
using MediaDeck.Composition.Interfaces.Tags;
using MediaDeck.Composition.Stores.Config.Model;
using MediaDeck.Database.Tables;
using MediaDeck.MediaItemTypes.Base;
using MediaDeck.MediaItemTypes.FolderGroup.Models;
using MediaDeck.MediaItemTypes.FolderGroup.ViewModels;

using Microsoft.Extensions.DependencyInjection;

namespace MediaDeck.MediaItemTypes.FolderGroup;

public class FolderGroupMediaItemFactoryCore : BaseMediaItemFactoryCore<FolderGroupMediaItemOperator, FolderGroupMediaItemModel, FolderGroupExecutionProgramObjectModel, FolderGroupMediaItemViewModel, FolderGroupExecutionProgramConfigViewModel, FolderGroupThumbnailPickerViewModel> {
	private readonly FolderGroupMediaItemOperator _fileOperator;
	private readonly IServiceProvider _serviceProvider;
	private readonly IMediaItemTypeProvider _mediaItemTypeProvider;

	public FolderGroupMediaItemFactoryCore(
		FolderGroupMediaItemOperator fileOperator,
		ConfigModel config,
		ITagsManager tagsManager,
		IMediaItemTypeProvider mediaItemTypeProvider,
		IServiceProvider serviceProvider)
		: base(config, tagsManager, MediaType.FolderGroup) {
		this._fileOperator = fileOperator;
		this._serviceProvider = serviceProvider;
		this._mediaItemTypeProvider = mediaItemTypeProvider;
	}

	public override FolderGroupMediaItemOperator CreateMediaItemOperator() {
		return this._fileOperator;
	}

	public override ItemType ItemType {
		get {
			return ItemType.FolderGroup;
		}
	}

	/// <inheritdoc />
	public override FolderGroupExecutionProgramObjectModel CreateExecutionProgramObjectModel() {
		var model = new FolderGroupExecutionProgramObjectModel();
		return model;
	}

	/// <inheritdoc />
	public override FolderGroupExecutionProgramConfigViewModel CreateExecutionProgramConfigViewModel(FolderGroupExecutionProgramObjectModel model) {
		return new FolderGroupExecutionProgramConfigViewModel(model, this._serviceProvider.GetRequiredService<IMediaItemTypeService>(), this._serviceProvider.GetRequiredService<ExecutionConfigModel>());
	}

	public override FolderGroupMediaItemModel CreateMediaItemModelFromRecord(MediaItem MediaItem, IServiceProvider scopedServiceProvider) {
		var model = new FolderGroupMediaItemModel(MediaItem.MediaItemId, MediaItem.FilePath, this._fileOperator, this._mediaItemTypeProvider, scopedServiceProvider);
		this.SetModelProperties(model, MediaItem);
		return model;
	}

	public override FolderGroupMediaItemViewModel CreateMediaItemViewModel(FolderGroupMediaItemModel fileModel) {
		return new FolderGroupMediaItemViewModel(fileModel, this);
	}

	public override FolderGroupThumbnailPickerViewModel CreateThumbnailPickerViewModel() {
		return this._serviceProvider.GetRequiredService<FolderGroupThumbnailPickerViewModel>();
	}
}