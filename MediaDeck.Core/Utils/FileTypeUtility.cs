using CommunityToolkit.Mvvm.DependencyInjection;

using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.FileTypes;
using MediaDeck.Composition.Interfaces.FileTypes.Models;
using MediaDeck.Composition.Interfaces.FileTypes.ViewModels;
using MediaDeck.Composition.Interfaces.FileTypes.Views;
using MediaDeck.Database.Tables;

using Microsoft.Extensions.DependencyInjection;

namespace MediaDeck.Core.Utils;

public static class FileTypeUtility {
	static FileTypeUtility() {
		_fileTypes = Ioc.Default.GetServices<IFileType>().ToArray();
		_unknownFileType = _fileTypes.First(x => x.MediaType == MediaType.Unknown);
		_filePathService = Ioc.Default.GetRequiredService<IFilePathService>();
	}

	private static readonly IFileType[] _fileTypes;
	private static readonly IFileType _unknownFileType;
	private static readonly IFilePathService _filePathService;

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
		return _fileTypes.FirstOrDefault(x => x.MediaType == _filePathService.GetMediaType(mediaFile.FilePath)) ?? _unknownFileType;
	}

	private static IFileType GetFileType(IFileModel fileModel) {
		return _fileTypes.FirstOrDefault(x => x.MediaType == fileModel.MediaType) ?? _unknownFileType;
	}

	private static IFileType GetFileType(IFileViewModel fileViewModel) {
		return _fileTypes.FirstOrDefault(x => x.MediaType == fileViewModel.MediaType) ?? _unknownFileType;
	}

	public static IQueryable<MediaFile> IncludeTables(this IQueryable<MediaFile> mediaFiles) {
		var result = mediaFiles;
		foreach (var fileType in _fileTypes) {
			result = fileType.IncludeTables(result);
		}
		return result;
	}
}