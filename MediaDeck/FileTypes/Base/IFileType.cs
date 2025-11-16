using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.FileTypes.Models;
using MediaDeck.Database.Tables;
using MediaDeck.FileTypes.Base.Models.Interfaces;
using MediaDeck.FileTypes.Base.ViewModels.Interfaces;
using MediaDeck.FileTypes.Base.Views.Interfaces;

namespace MediaDeck.FileTypes.Base;
public interface IFileType<TFileOperator, TFileModel, TFileViewModel, TDetailViewerPreviewControlView, TThumbnailPickerViewModel, TThumbnailPickerView>:IFileType
	where TFileOperator: IFileOperator
	where TFileModel: IFileModel
	where TFileViewModel : IFileViewModel
	where TDetailViewerPreviewControlView: IDetailViewerPreviewControlView
	where TThumbnailPickerViewModel: IThumbnailPickerViewModel
	where TThumbnailPickerView: IThumbnailPickerView {
	public new TFileOperator CreateFileOperator();
	public new TFileModel CreateFileModelFromRecord(MediaFile mediaFile);
	public TDetailViewerPreviewControlView CreateDetailViewerPreviewControlView(TFileViewModel fileViewModel);
	public TFileViewModel CreateFileViewModel(TFileModel fileModel);
	public new TThumbnailPickerViewModel CreateThumbnailPickerViewModel();
	public new TThumbnailPickerView CreateThumbnailPickerView();
}
public interface IFileType {
	public MediaType MediaType {
		get;
	}
	public IFileOperator CreateFileOperator();
	public IFileModel CreateFileModelFromRecord(MediaFile mediaFile);
	public IDetailViewerPreviewControlView CreateDetailViewerPreviewControlView(IFileViewModel fileViewModel);
	public IFileViewModel CreateFileViewModel(IFileModel fileModel);
	public IThumbnailPickerViewModel CreateThumbnailPickerViewModel();
	public IThumbnailPickerView CreateThumbnailPickerView();
	public IQueryable<MediaFile> IncludeTables(IQueryable<MediaFile> mediaFiles);
}
