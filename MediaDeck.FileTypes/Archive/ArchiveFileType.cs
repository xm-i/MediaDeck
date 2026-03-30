using CommunityToolkit.Mvvm.DependencyInjection;

using MediaDeck.Database.Tables;
using MediaDeck.FileTypes.Base;
using MediaDeck.FileTypes.Archive.Models;
using MediaDeck.FileTypes.Archive.ViewModels;
using MediaDeck.FileTypes.Archive.Views;
using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.FileTypes;

namespace MediaDeck.FileTypes.Archive;

[Inject(InjectServiceLifetime.Transient, typeof(IFileType))]
internal class ArchiveFileType : BaseFileType<ArchiveFileOperator, ArchiveFileModel, ArchiveFileViewModel, ArchiveDetailViewerPreviewControlView, ArchiveThumbnailPickerViewModel, ArchiveThumbnailPickerView> {
	private ArchiveDetailViewerPreviewControlView? _archiveDetailViewerPreviewControlView;
	private readonly ArchiveFileOperator _archiveFileOperator;

	public ArchiveFileType(ArchiveFileOperator archiveFileOperator) : base(MediaType.Archive) {
		this._archiveFileOperator = archiveFileOperator;
	}

	public override ArchiveFileOperator CreateFileOperator() {
		return this._archiveFileOperator;
	}

	public override ArchiveFileModel CreateFileModelFromRecord(MediaFile mediaFile) {
		var ifm = new ArchiveFileModel(mediaFile.MediaFileId, mediaFile.FilePath, this._archiveFileOperator);
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
		return Ioc.Default.GetRequiredService<ArchiveThumbnailPickerViewModel>();
	}

	public override ArchiveThumbnailPickerView CreateThumbnailPickerView() {
		return new ArchiveThumbnailPickerView();
	}

	public override IQueryable<MediaFile> IncludeTables(IQueryable<MediaFile> mediaFiles) {
		return mediaFiles
			.Include(mf => mf.Container);
	}
}