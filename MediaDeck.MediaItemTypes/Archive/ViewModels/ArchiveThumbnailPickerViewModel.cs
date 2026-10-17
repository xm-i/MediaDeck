using System.IO.Compression;
using System.Threading.Tasks;
using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.MediaItemTypes;
using MediaDeck.Composition.Interfaces.MediaItemTypes.ViewModels;
using MediaDeck.Composition.Stores.Config.Model;
using MediaDeck.MediaItemTypes.Archive.Models;
using MediaDeck.MediaItemTypes.Base.Models;
using MediaDeck.MediaItemTypes.Base.ViewModels;
using Microsoft.Extensions.Logging;

namespace MediaDeck.MediaItemTypes.Archive.ViewModels;

[Inject(InjectServiceLifetime.Transient)]
public class ArchiveThumbnailPickerViewModel : BaseThumbnailPickerViewModel<BaseThumbnailPickerModel> {
	private readonly ArchiveMediaItemOperator _ArchiveMediaItemOperator;
	private readonly IMediaItemTypeService _mediaItemTypeService;
	private readonly ILogger<ArchiveThumbnailPickerViewModel> _logger;

	private readonly ObservableList<string> _entries = [];

	public ArchiveThumbnailPickerViewModel(BaseThumbnailPickerModel thumbnailPickerModel, ArchiveMediaItemOperator PdfMediaItemOperator, IMediaItemTypeService mediaItemTypeService, ILogger<ArchiveThumbnailPickerViewModel> logger, IFilePickerService filePickerService, ConfigModel config) : base(thumbnailPickerModel, filePickerService, config) {
		this._ArchiveMediaItemOperator = PdfMediaItemOperator;
		this._mediaItemTypeService = mediaItemTypeService;
		this._logger = logger;
		this.Entries = this._entries.ToNotifyCollectionChanged(SynchronizationContextCollectionEventDispatcher.Current).AddTo(this.CompositeDisposable);
		this.SelectedEntry.SubscribeAwait(async (x, ct) => {
			if (x is null) {
				this.FileName.Value = null;
			} else {
				this.FileName.Value = x;
				await this.RecreateThumbnailAsync();
			}
		}, AwaitOperation.Drop).AddTo(this.CompositeDisposable);
	}

	public BindableReactiveProperty<string?> FileName {
		get;
	} = new();

	public INotifyCollectionChangedSynchronizedViewList<string> Entries {
		get;
	}

	public BindableReactiveProperty<string?> SelectedEntry {
		get;
	} = new();

	public override Task RecreateThumbnailAsync() {
		if (this.targetFileViewModel is null) {
			return Task.CompletedTask;
		}
		if (this.FileName.Value is null) {
			this.CandidateThumbnail.Value = null;
			return Task.CompletedTask;
		}

		using var archive = ZipFile.OpenRead(this.targetFileViewModel!.FileModel.FilePath);
		if (!archive.Entries.Any(x => x.FullName == this.FileName.Value)) {
			this.CandidateThumbnail.Value = null;
			return Task.CompletedTask;
		}

		try {
			this.CandidateThumbnail.Value = this._ArchiveMediaItemOperator.CreateThumbnail(archive, (uint)this._config.ThumbnailConfig.ThumbnailSize.Value, (uint)this._config.ThumbnailConfig.ThumbnailSize.Value, this.FileName.Value);
		} catch (Exception ex) {
			this._logger.LogError(ex, "Failed to recreate archive thumbnail for file {FilePath} at entry {EntryName}", this.targetFileViewModel.FilePath, this.FileName.Value);
			this.CandidateThumbnail.Value = null;
		}
		return Task.CompletedTask;
	}

	public override async Task LoadAsync(IMediaItemViewModel fileViewModel) {
		await base.LoadAsync(fileViewModel);
		this._entries.Clear();
		using var archive = ZipFile.OpenRead(fileViewModel.FileModel.FilePath);
		this._entries.AddRange(archive.Entries.Where(x => this._mediaItemTypeService.IsTargetPath(x.Name, MediaType.Image)).Select(x => x.FullName).ToList());
	}
}