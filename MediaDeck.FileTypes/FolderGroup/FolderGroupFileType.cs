using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.FileTypes;
using MediaDeck.Composition.Interfaces.Tags;
using MediaDeck.Composition.Stores.Config.Model;
using MediaDeck.Database.Tables;
using MediaDeck.FileTypes.Base;
using MediaDeck.FileTypes.FolderGroup.Models;
using MediaDeck.FileTypes.FolderGroup.ViewModels;
using MediaDeck.FileTypes.FolderGroup.Views;
using Microsoft.Extensions.DependencyInjection;

namespace MediaDeck.FileTypes.FolderGroup;

[Inject(InjectServiceLifetime.Singleton, typeof(IFileType))]
internal class FolderGroupFileType : BaseFileType<FolderGroupFileOperator, FolderGroupFileModel, FolderGroupFileViewModel, FolderGroupDetailViewerPreviewControlView, FolderGroupThumbnailPickerViewModel, FolderGroupThumbnailPickerView> {
	private FolderGroupDetailViewerPreviewControlView? _detailViewerPreviewControlView;
	private readonly FolderGroupFileOperator _fileOperator;
	private readonly IServiceProvider _serviceProvider;

	public FolderGroupFileType(
		FolderGroupFileOperator fileOperator,
		ConfigModel config,
		ITagsManager tagsManager,
		IServiceProvider serviceProvider)
		: base(config, tagsManager, MediaType.FolderGroup) {
		this._fileOperator = fileOperator;
		this._serviceProvider = serviceProvider;
	}

	public override FolderGroupFileOperator CreateFileOperator() {
		return this._fileOperator;
	}

	public override FolderGroupFileModel CreateFileModelFromRecord(MediaFile mediaFile) {
		var model = new FolderGroupFileModel(mediaFile.MediaFileId, mediaFile.FilePath, this._fileOperator, this._config);
		this.SetModelProperties(model, mediaFile);
		return model;
	}

	public override FolderGroupFileViewModel CreateFileViewModel(FolderGroupFileModel fileModel) {
		return new FolderGroupFileViewModel(fileModel);
	}

	public override FolderGroupDetailViewerPreviewControlView CreateDetailViewerPreviewControlView(FolderGroupFileViewModel fileViewModel) {
		return this._detailViewerPreviewControlView ??= new FolderGroupDetailViewerPreviewControlView();
	}

	public override FolderGroupThumbnailPickerViewModel CreateThumbnailPickerViewModel() {
		return this._serviceProvider.GetRequiredService<FolderGroupThumbnailPickerViewModel>();
	}

	public override FolderGroupThumbnailPickerView CreateThumbnailPickerView() {
		return new FolderGroupThumbnailPickerView();
	}

	public override IQueryable<MediaFile> IncludeTables(IQueryable<MediaFile> mediaFiles) {
		return mediaFiles.Include(mf => mf.FolderGroupMetadata);
	}
}
