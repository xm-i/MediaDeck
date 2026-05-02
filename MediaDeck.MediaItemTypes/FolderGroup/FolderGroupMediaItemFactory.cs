using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.MediaItemTypes;
using MediaDeck.Composition.Interfaces.MediaItemTypes.ViewModels;
using MediaDeck.Composition.Interfaces.Tags;
using MediaDeck.Composition.Stores.Config.Model;
using MediaDeck.Composition.Tables;
using MediaDeck.MediaItemTypes.Base;
using MediaDeck.MediaItemTypes.FolderGroup.Models;
using MediaDeck.MediaItemTypes.FolderGroup.ViewModels;

using Microsoft.Extensions.DependencyInjection;

namespace MediaDeck.MediaItemTypes.FolderGroup;

[Inject(InjectServiceLifetime.Scoped, typeof(IMediaItemFactory))]
[Inject(InjectServiceLifetime.Scoped, typeof(IMediaItemFactoryOf<FolderGroupMediaItemViewModel>))]
public class FolderGroupMediaItemFactory :
	BaseMediaItemFactory<FolderGroupMediaItemOperator, FolderGroupMediaItemModel, FolderGroupExecutionProgramObjectModel, FolderGroupMediaItemViewModel, FolderGroupExecutionProgramConfigViewModel, FolderGroupThumbnailPickerViewModel>,
	IMediaItemFactory<FolderGroupMediaItemOperator, FolderGroupMediaItemModel, FolderGroupExecutionProgramObjectModel, FolderGroupMediaItemViewModel, FolderGroupExecutionProgramConfigViewModel, FolderGroupThumbnailPickerViewModel>,
	IMediaItemFactoryOf<FolderGroupMediaItemViewModel> {
	private readonly FolderGroupMediaItemOperator _fileOperator;
	private readonly IServiceProvider _serviceProvider;

	public FolderGroupMediaItemFactory(
		FolderGroupMediaItemOperator fileOperator,
		ConfigModel config,
		ITagsManager tagsManager,
		IServiceProvider serviceProvider)
		: base(config, tagsManager, MediaType.FolderGroup) {
		this._fileOperator = fileOperator;
		this._serviceProvider = serviceProvider;
	}

	public override FolderGroupMediaItemOperator CreateMediaItemOperator() {
		return this._fileOperator;
	}

	public override FolderGroupMediaItemModel CreateMediaItemModelFromRecord(MediaItem MediaItem) {
		var model = this._serviceProvider.GetRequiredService<FolderGroupMediaItemModel>();
		model.Initialize(MediaItem.MediaItemId, MediaItem.FilePath);
		this.SetModelProperties(model, MediaItem);
		return model;
	}

	public override FolderGroupMediaItemViewModel CreateMediaItemViewModel(FolderGroupMediaItemModel fileModel) {
		var vm = this._serviceProvider.GetRequiredService<FolderGroupMediaItemViewModel>();
		vm.Initialize(fileModel);
		return vm;
	}

	public override FolderGroupThumbnailPickerViewModel CreateThumbnailPickerViewModel() {
		return this._serviceProvider.GetRequiredService<FolderGroupThumbnailPickerViewModel>();
	}

	/// <inheritdoc />
	public override FolderGroupExecutionProgramObjectModel CreateExecutionProgramObjectModel() {
		var model = this._serviceProvider.GetRequiredService<FolderGroupExecutionProgramObjectModel>();
		model.MediaType = this.MediaType;
		return model;
	}

	/// <inheritdoc />
	public override FolderGroupExecutionProgramConfigViewModel CreateExecutionProgramConfigViewModel(FolderGroupExecutionProgramObjectModel model) {
		var vm = this._serviceProvider.GetRequiredService<FolderGroupExecutionProgramConfigViewModel>();
		vm.Initialize(model);
		return vm;
	}

	public override IBulkThumbnailConfigViewModel CreateBulkThumbnailConfigViewModel() {
		return this._serviceProvider.GetRequiredService<FolderGroupBulkThumbnailConfigViewModel>();
	}

}