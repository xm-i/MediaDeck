using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.FileTypes;
using MediaDeck.Composition.Interfaces.FileTypes.Models;
using MediaDeck.Composition.Interfaces.FileTypes.ViewModels;
using MediaDeck.Composition.Interfaces.FileTypes.Views;
using MediaDeck.Database.Tables;

namespace MediaDeck.Core.Services;

/// <summary>
/// ファイルタイプに関連する操作を提供するサービス実装クラス
/// </summary>
[Inject(InjectServiceLifetime.Singleton, typeof(IFileTypeService))]
public class FileTypeService(IEnumerable<IFileType> fileTypes, IFilePathService filePathService) : IFileTypeService {
	private readonly IFileType[] _fileTypes = fileTypes.ToArray();
	private readonly IFileType _unknownFileType = fileTypes.First(x => x.MediaType == MediaType.Unknown);
	private readonly IFilePathService _filePathService = filePathService;

	/// <inheritdoc />
	public IFileModel CreateFileModelFromRecord(MediaFile mediaFile) {
		return this.GetFileType(mediaFile).CreateFileModelFromRecord(mediaFile);
	}

	/// <inheritdoc />
	public IFileViewModel CreateFileViewModel(IFileModel fileModel) {
		return this.GetFileType(fileModel).CreateFileViewModel(fileModel);
	}

	/// <inheritdoc />
	public IDetailViewerPreviewControlView CreateDetailViewerPreviewControlView(IFileViewModel fileViewModel) {
		return this.GetFileType(fileViewModel).CreateDetailViewerPreviewControlView(fileViewModel);
	}

	/// <inheritdoc />
	public IThumbnailPickerViewModel CreateThumbnailPickerViewModel(IFileViewModel fileViewModel) {
		return this.GetFileType(fileViewModel).CreateThumbnailPickerViewModel();
	}

	/// <inheritdoc />
	public IThumbnailPickerView CreateThumbnailPickerView(IFileViewModel fileViewModel) {
		return this.GetFileType(fileViewModel).CreateThumbnailPickerView();
	}

	/// <inheritdoc />
	public IFileOperator[] CreateFileOperators() {
		return this._fileTypes.Select(x => x.CreateFileOperator()).ToArray();
	}

	/// <inheritdoc />
	public IQueryable<MediaFile> IncludeTables(IQueryable<MediaFile> mediaFiles) {
		var result = mediaFiles;
		foreach (var fileType in this._fileTypes) {
			result = fileType.IncludeTables(result);
		}
		return result;
	}

	private IFileType GetFileType(MediaFile mediaFile) {
		return this._fileTypes.FirstOrDefault(x => x.MediaType == this._filePathService.GetMediaType(mediaFile.FilePath)) ?? this._unknownFileType;
	}

	private IFileType GetFileType(IFileModel fileModel) {
		return this._fileTypes.FirstOrDefault(x => x.MediaType == fileModel.MediaType) ?? this._unknownFileType;
	}

	private IFileType GetFileType(IFileViewModel fileViewModel) {
		return this._fileTypes.FirstOrDefault(x => x.MediaType == fileViewModel.MediaType) ?? this._unknownFileType;
	}
}