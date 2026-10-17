using MediaDeck.Common.Base;
using MediaDeck.Common.Collections;
using MediaDeck.Common.Extensions;
using MediaDeck.Composition.Interfaces.MediaItemTypes;
using MediaDeck.Composition.Interfaces.MediaItemTypes.ViewModels;
using MediaDeck.Composition.Interfaces.Notifications;
using MediaDeck.Core.Models.Files;
using MediaDeck.Core.Models.Repositories;
using MediaDeck.Core.Models.Tools;

namespace MediaDeck.ViewModels.Panes.ViewerPanes;

[Inject(InjectServiceLifetime.Scoped)]
public class MediaContentLibraryViewModel : ViewModelBase {
	private readonly AlbumRepository _albumRepository;
	private readonly BackgroundTasksModel _backgroundTasksModel;

	public MediaContentLibraryViewModel(MediaContentLibrary mediaContentLibrary, SearchConditionManagerViewModel searchConditionManagerViewModel, IMediaItemTypeService MediaItemTypeService, AlbumRepository albumRepository, BackgroundTasksModel backgroundTasksModel) {
		this._albumRepository = albumRepository;
		this._backgroundTasksModel = backgroundTasksModel;
		this.SearchConditionManagerViewModel = searchConditionManagerViewModel;
		this.Files = mediaContentLibrary.Files
			.CreateViewWithDisposal(MediaItemTypeService.CreateMediaItemViewModel)
			.ToNotifyCollectionChangedWithDisposal(SynchronizationContextCollectionEventDispatcher.Current)
			.AddTo(this.CompositeDisposable);
		this.SearchElapsedMilliseconds = mediaContentLibrary.SearchElapsedMilliseconds.ObserveOnCurrentSynchronizationContext().ToBindableReactiveProperty().AddTo(this.CompositeDisposable);
		this.CanLoadMore = mediaContentLibrary.CanLoadMore.ObserveOnCurrentSynchronizationContext().ToBindableReactiveProperty().AddTo(this.CompositeDisposable);
		this.TotalCount = mediaContentLibrary.TotalCount.ObserveOnCurrentSynchronizationContext().ToBindableReactiveProperty().AddTo(this.CompositeDisposable);

		this.LoadMoreCommand = new ReactiveCommand().AddTo(this.CompositeDisposable);
		this.LoadMoreCommand
			.SubscribeAwait(async (_, ct) => {
				await mediaContentLibrary.LoadMoreAsync(ct).ConfigureAwait(false);
			}, AwaitOperation.Drop)
			.AddTo(this.CompositeDisposable);
	}

	public ReactiveCommand LoadMoreCommand {
		get;
	}

	public BindableReactiveProperty<bool> CanLoadMore {
		get;
	}

	public BindableReactiveProperty<int?> TotalCount {
		get;
	}

	public SearchConditionManagerViewModel SearchConditionManagerViewModel {
		get;
	}

	public DisposingNotifyCollectionChangedSynchronizedViewList<IMediaItemViewModel> Files {
		get;
	}

	public INotifyCollectionChangedSynchronizedViewList<SearchConditionViewModel> SearchConditions {
		get {
			return this.SearchConditionManagerViewModel.SearchConditions;
		}
	}

	public INotifyCollectionChangedSynchronizedViewList<SearchConditionViewModel> FilteredSearchConditionCandidates {
		get {
			return this.SearchConditionManagerViewModel.FilteredSearchConditionCandidates;
		}
	}

	public BindableReactiveProperty<IMediaItemViewModel> SelectedFile {
		get;
	} = new();

	public BindableReactiveProperty<IMediaItemViewModel[]> SelectedFiles {
		get;
	} = new([]);

	public BindableReactiveProperty<long?> SearchElapsedMilliseconds {
		get;
	}

	public ISearchConditionNotificationDispatcher SearchConditionNotificationDispatcher {
		get {
			return this.SearchConditionManagerViewModel.SearchConditionNotificationDispatcher;
		}
	}

	public void RefreshSearchTokenCandidates(string word) {
		this.SearchConditionManagerViewModel.RefreshSearchTokenCandidates(word);
	}

	public void Reload() {
		this.SearchConditionManagerViewModel.Reload();
	}

	public async Task<IReadOnlyList<string>> GetRecentAlbumPathsAsync(int max = 12) {
		var albums = await this._albumRepository.GetRecentAlbumsAsync(max);
		return albums.Select(x => x.Path).ToList();
	}

	public async Task AddToAlbumAsync(string albumPath, IEnumerable<IMediaItemViewModel> mediaItems) {
		var ids = mediaItems.Select(x => x.FileModel.Id).Distinct().ToArray();
		if (ids.Length == 0) {
			return;
		}
		await this._albumRepository.AddItemsAsync(albumPath, ids);
	}

	/// <summary>
	/// 指定されたメディアアイテムのメタデータ更新をキューに追加します。
	/// </summary>
	/// <param name="mediaItems">更新対象のメディアアイテム</param>
	public void UpdateMetadata(IEnumerable<IMediaItemViewModel> mediaItems) {
		var ids = mediaItems.Select(x => x.FileModel.Id).Distinct().ToArray();
		if (ids.Length == 0) {
			return;
		}
		this._backgroundTasksModel.EnqueueMetadataUpdate(ids);
	}
}