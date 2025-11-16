using MediaDeck.FileTypes.Base.ViewModels;
using MediaDeck.FileTypes.Archive.Models;
using MediaDeck.Models.FileDetailManagers;
using System.IO.Compression;
using System.Threading.Tasks;
using Windows.System.Threading;
using MediaDeck.FileTypes.Base.ViewModels.Interfaces;

namespace MediaDeck.FileTypes.Archive.ViewModels;

[AddTransient]
public class ArchiveThumbnailPickerViewModel : BaseThumbnailPickerViewModel {
	private readonly ArchiveFileOperator _archiveFileOperator;

	public ArchiveThumbnailPickerViewModel(ThumbnailsManager thumbnailsManager, ArchiveFileOperator pdfFileOperator) : base(thumbnailsManager) {
		this._archiveFileOperator = pdfFileOperator;
		this.SelectedEntry.Subscribe(x => {
			if (x is null) {
				this.FileName.Value = null;
			} else {
				this.FileName.Value = x;
				this.RecreateThumbnail();
			}
		});
	}

	public BindableReactiveProperty<string?> FileName {
		get;
	} = new();

	public ObservableList<string> Entries {
		get;
	} = [];

	public BindableReactiveProperty<string?> SelectedEntry {
		get;
	} = new();

	public override void RecreateThumbnail() {
		using var archive = ZipFile.OpenRead(this.targetFileViewModel!.FileModel.FilePath);
		if (this.targetFileViewModel is null) {
			return;
		}
		if(this.FileName.Value is null) {
			this.CandidateThumbnail.Value = null;
			return;
		}
		if(!archive.Entries.Any(x => x.FullName == this.FileName.Value)) {
			this.CandidateThumbnail.Value = null;
			return;
		}

		try {
			this.CandidateThumbnail.Value = this._archiveFileOperator.CreateThumbnail(archive, 300, 300, this.FileName.Value);
		} catch (Exception) {
			this.CandidateThumbnail.Value = null;
		}
	}

	public override async Task LoadAsync(IFileViewModel fileViewModel) {
		await base.LoadAsync(fileViewModel);
		this.Entries.Clear();
		using var archive = ZipFile.OpenRead(fileViewModel.FileModel.FilePath);
		this.Entries.AddRange(archive.Entries.Where(x => FilePathUtility.IsImageFile(x.Name)).Select(x => x.FullName).ToList());
	}
}
