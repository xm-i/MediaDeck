using CommunityToolkit.Mvvm.DependencyInjection;

using Microsoft.Extensions.DependencyInjection;

using MediaDeck.Database.Tables;
using MediaDeck.FileTypes.Base;
using MediaDeck.FileTypes.Base.Models.Interfaces;
using MediaDeck.FileTypes.Base.ViewModels.Interfaces;
using MediaDeck.FileTypes.Base.Views;

namespace MediaDeck.Utils.Tools;
public static class FileTypeUtility {
	static FileTypeUtility() {
		_fileTypes = Ioc.Default.GetServices<IFileType>().ToArray();
	}
	private static readonly IFileType[] _fileTypes;
	public static IFileModel CreateFileModelFromRecord(MediaFile mediaFile) {
		return GetFileType(mediaFile).CreateFileModelFromRecord(mediaFile);
	}

	public static IFileViewModel CreateFileViewModel(IFileModel fileModel) {
		return GetFileType(fileModel).CreateFileViewModel(fileModel);
	}

	public static IDetailViewerPreviewControlView CreateDetailViewerPreviewControlView(IFileViewModel fileViewModel) {
		return GetFileType(fileViewModel).CreateDetailViewerPreviewControlView(fileViewModel);
	}

	public static IThumbnailPickerViewModel CreateThumbnailPickerViewModel(IFileViewModel fileViewModel) {
		return GetFileType(fileViewModel).CreateThumbnailPickerViewModel();
	}

	public static IThumbnailPickerView CreateThumbnailPickerView(IFileViewModel fileViewModel) {
		return GetFileType(fileViewModel).CreateThumbnailPickerView();
	}

	public static IFileOperator[] CreateFileOperators() {
		return _fileTypes.Select(x => x.CreateFileOperator()).ToArray();
	}

	private static IFileType GetFileType(MediaFile mediaFile) {
		return _fileTypes.First(x => x.MediaType == mediaFile.FilePath.GetMediaType());
	}
	private static IFileType GetFileType(IFileModel fileModel) {
		return _fileTypes.First(x => x.MediaType == fileModel.MediaType);
	}
	private static IFileType GetFileType(IFileViewModel fileViewModel) {
		return _fileTypes.First(x => x.MediaType == fileViewModel.MediaType);
	}

	public static IQueryable<MediaFile> IncludeTables(this IQueryable<MediaFile> mediaFiles) {
		var result = mediaFiles;
		foreach (var fileType in _fileTypes) {
			result = fileType.IncludeTables(result);
		}
		return result;
	}
}
