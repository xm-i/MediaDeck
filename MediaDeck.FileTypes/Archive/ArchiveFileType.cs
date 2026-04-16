using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.FileTypes;
using MediaDeck.Composition.Interfaces.Tags;
using MediaDeck.Composition.Stores.Config.Model;
using MediaDeck.Database.Tables;
using MediaDeck.FileTypes.Archive.Models;
using MediaDeck.FileTypes.Archive.ViewModels;
using MediaDeck.FileTypes.Archive.Views;
using MediaDeck.FileTypes.Base;
using Microsoft.Extensions.DependencyInjection;

namespace MediaDeck.FileTypes.Archive;

[Inject(InjectServiceLifetime.Transient, typeof(IFileType))]
internal class ArchiveFileType : BaseFileType<ArchiveFileOperator, ArchiveFileModel, ArchiveFileViewModel, ArchiveDetailViewerPreviewControlView, ArchiveThumbnailPickerViewModel, ArchiveThumbnailPickerView> {
	private ArchiveDetailViewerPreviewControlView? _archiveDetailViewerPreviewControlView;
	private readonly ArchiveFileOperator _archiveFileOperator;
	private readonly IServiceProvider _serviceProvider;

	public ArchiveFileType(
		ArchiveFileOperator archiveFileOperator,
		ConfigModel config,
		ITagsManager tagsManager,
		IServiceProvider serviceProvider)
		: base(config, tagsManager, MediaType.Archive) {
		this._archiveFileOperator = archiveFileOperator;
		this._serviceProvider = serviceProvider;
	}

	public override ArchiveFileOperator CreateFileOperator() {
		return this._archiveFileOperator;
	}

	public override ArchiveFileModel CreateFileModelFromRecord(MediaFile mediaFile) {
		var ifm = new ArchiveFileModel(mediaFile.MediaFileId, mediaFile.FilePath, this._archiveFileOperator, this._config);
		this.SetModelProperties(ifm, mediaFile);
		return ifm;
	}

	public override ArchiveFileViewModel CreateFileViewModel(ArchiveFileModel fileModel) {
		return new ArchiveFileViewModel(fileModel);
	}

	public override ArchiveDetailViewerPreviewControlView CreateDetailViewerPreviewControlView(ArchiveFileViewModel fileViewModel) {
		return this._archiveDetailViewerPreviewControlView ??= new ArchiveDetailViewerPreviewControlView();
	}

	public override ArchiveThumbnailPickerViewModel CreateThumbnailPickerViewModel() {
		return this._serviceProvider.GetRequiredService<ArchiveThumbnailPickerViewModel>();
	}

	public override ArchiveThumbnailPickerView CreateThumbnailPickerView() {
		return new ArchiveThumbnailPickerView();
	}

	public override IQueryable<MediaFile> IncludeTables(IQueryable<MediaFile> mediaFiles) {
		return mediaFiles
			.Include(mf => mf.Container);
	}
}