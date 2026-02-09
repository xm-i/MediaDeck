using CommunityToolkit.Mvvm.DependencyInjection;

using MediaDeck.Composition.Enum;
using MediaDeck.Database.Tables;
using MediaDeck.FileTypes.Base;
using MediaDeck.FileTypes.Unknown.Models;
using MediaDeck.FileTypes.Unknown.ViewModels;
using MediaDeck.FileTypes.Unknown.Views;

namespace MediaDeck.FileTypes.Unknown;
[Inject(InjectServiceLifetime.Transient, typeof(IFileType))]
public class UnknownFileType : BaseFileType<UnknownFileOperator, UnknownFileModel, UnknownFileViewModel, UnknownDetailViewerPreviewControlView, UnknownThumbnailPickerViewModel, UnknownThumbnailPickerView> {
	private UnknownDetailViewerPreviewControlView? _unknownDetailViewerPreviewControlView;
	public override MediaType MediaType {
		get;
	} = MediaType.Unknown;

	public override UnknownFileOperator CreateFileOperator() {
		return new UnknownFileOperator();
	}

	public override UnknownFileModel CreateFileModelFromRecord(MediaFile mediaFile) {
		var ifm = new UnknownFileModel(mediaFile.MediaFileId, mediaFile.FilePath);
		this.SetModelProperties(ifm, mediaFile);
		return ifm;
	}

	public override UnknownFileViewModel CreateFileViewModel(UnknownFileModel fileModel) {
		return new UnknownFileViewModel(fileModel);
	}
	public override UnknownDetailViewerPreviewControlView CreateDetailViewerPreviewControlView(UnknownFileViewModel fileViewModel) {
		return this._unknownDetailViewerPreviewControlView ??= new UnknownDetailViewerPreviewControlView();
	}

	public override UnknownThumbnailPickerViewModel CreateThumbnailPickerViewModel() {
		return Ioc.Default.GetRequiredService<UnknownThumbnailPickerViewModel>();
	}

	public override UnknownThumbnailPickerView CreateThumbnailPickerView() {
		return new UnknownThumbnailPickerView();
	}

	public override IQueryable<MediaFile> IncludeTables(IQueryable<MediaFile> mediaFiles) {
		return mediaFiles;
	}
}
