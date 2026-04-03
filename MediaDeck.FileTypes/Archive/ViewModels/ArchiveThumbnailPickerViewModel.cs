using System.IO.Compression;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using MediaDeck.Composition.Interfaces.FileTypes.ViewModels;
using MediaDeck.FileTypes.Archive.Models;
using MediaDeck.FileTypes.Base.Models;
using MediaDeck.FileTypes.Base.ViewModels;

namespace MediaDeck.FileTypes.Archive.ViewModels;

[Inject(InjectServiceLifetime.Transient)]
internal class ArchiveThumbnailPickerViewModel : BaseThumbnailPickerViewModel {
	private readonly ArchiveFileOperator _archiveFileOperator;
	private readonly IFilePathService _filePathService;
	private readonly ILogger<ArchiveThumbnailPickerViewModel> _logger;

	public ArchiveThumbnailPickerViewModel(BaseThumbnailPickerModel thumbnailPickerModel, ArchiveFileOperator pdfFileOperator, IFilePathService filePathService, ILogger<ArchiveThumbnailPickerViewModel> logger) : base(thumbnailPickerModel) {
		this._archiveFileOperator = pdfFileOperator;
		this._filePathService = filePathService;
		this._logger = logger;
		this.SelectedEntry.Subscribe(x => {
			if (x is null) {
				this.FileName.Value = null;
			} else {
				this.FileName.Value = x;
				this.RecreateThumbnail();
			}
		});
	}

	internal BindableReactiveProperty<string?> FileName {
		get;
	} = new();

	internal ObservableList<string> Entries {
		get;
	} = [];

	internal BindableReactiveProperty<string?> SelectedEntry {
		get;
	} = new();

	public override void RecreateThumbnail() {
		using var archive = ZipFile.OpenRead(this.targetFileViewModel!.FileModel.FilePath);
		if (this.targetFileViewModel is null) {
			return;
		}
		if (this.FileName.Value is null) {
			this.CandidateThumbnail.Value = null;
			return;
		}
		if (!archive.Entries.Any(x => x.FullName == this.FileName.Value)) {
			this.CandidateThumbnail.Value = null;
			return;
		}

		try {
			this.CandidateThumbnail.Value = this._archiveFileOperator.CreateThumbnail(archive, 300, 300, this.FileName.Value);
		} catch (Exception ex) {
			this._logger.LogError(ex, "Failed to recreate archive thumbnail for file {FilePath} at entry {EntryName}", this.targetFileViewModel.FilePath, this.FileName.Value);
			this.CandidateThumbnail.Value = null;
		}
	}

	public override async Task LoadAsync(IFileViewModel fileViewModel) {
		await base.LoadAsync(fileViewModel);
		this.Entries.Clear();
		using var archive = ZipFile.OpenRead(fileViewModel.FileModel.FilePath);
		this.Entries.AddRange(archive.Entries.Where(x => this._filePathService.IsImageFile(x.Name)).Select(x => x.FullName).ToList());
	}
}