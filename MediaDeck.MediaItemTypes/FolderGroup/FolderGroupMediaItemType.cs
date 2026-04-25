using System.IO;

using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.MediaItemTypes;
using MediaDeck.Composition.Interfaces.Tags;
using MediaDeck.Composition.Stores.Config.Model;
using MediaDeck.Database.Tables;
using MediaDeck.MediaItemTypes.Base;
using MediaDeck.MediaItemTypes.FolderGroup.Models;
using MediaDeck.MediaItemTypes.FolderGroup.ViewModels;
using MediaDeck.MediaItemTypes.FolderGroup.Views;
using Microsoft.Extensions.DependencyInjection;

namespace MediaDeck.MediaItemTypes.FolderGroup;

[Inject(InjectServiceLifetime.Singleton, typeof(IMediaItemType))]
internal class FolderGroupMediaItemType : BaseMediaItemType<FolderGroupMediaItemOperator, FolderGroupMediaItemModel, FolderGroupMediaItemViewModel, FolderGroupDetailViewerPreviewControlView, FolderGroupThumbnailPickerViewModel, FolderGroupThumbnailPickerView> {
	private FolderGroupDetailViewerPreviewControlView? _detailViewerPreviewControlView;
	private readonly FolderGroupMediaItemOperator _fileOperator;
	private readonly IServiceProvider _serviceProvider;

	public FolderGroupMediaItemType(
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

	public override ItemType ItemType {
		get {
			return MediaDeck.Database.Tables.ItemType.FolderGroup;
		}
	}

	public override bool IsTargetPath(string path) {
		return Directory.Exists(path);
	}

	public override MediaItemPathStatus GetPathStatus(string path) {
		var directoryInfo = new DirectoryInfo(path);
		if (!directoryInfo.Exists) {
			return new(false, 0, DateTime.MinValue, DateTime.MinValue, DateTime.MinValue);
		}

		return new(true, 0, directoryInfo.CreationTime, directoryInfo.LastWriteTime, directoryInfo.LastAccessTime);
	}

	public override FolderGroupMediaItemModel CreateMediaItemModelFromRecord(MediaItem MediaItem) {
		var model = new FolderGroupMediaItemModel(MediaItem.MediaItemId, MediaItem.FilePath, this._fileOperator, this._config);
		this.SetModelProperties(model, MediaItem);
		return model;
	}

	public override FolderGroupMediaItemViewModel CreateMediaItemViewModel(FolderGroupMediaItemModel fileModel) {
		return new FolderGroupMediaItemViewModel(fileModel);
	}

	public override FolderGroupDetailViewerPreviewControlView CreateDetailViewerPreviewControlView(FolderGroupMediaItemViewModel fileViewModel) {
		return this._detailViewerPreviewControlView ??= new FolderGroupDetailViewerPreviewControlView();
	}

	public override FolderGroupThumbnailPickerViewModel CreateThumbnailPickerViewModel() {
		return this._serviceProvider.GetRequiredService<FolderGroupThumbnailPickerViewModel>();
	}

	public override FolderGroupThumbnailPickerView CreateThumbnailPickerView() {
		return new FolderGroupThumbnailPickerView();
	}

	public override IQueryable<MediaItem> IncludeTables(IQueryable<MediaItem> MediaItems) {
		return MediaItems.Include(mf => mf.FolderGroupMetadata);
	}
}