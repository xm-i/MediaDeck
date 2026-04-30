using MediaDeck.Common.Base;
using MediaDeck.Composition.Interfaces.MediaItemTypes;
using MediaDeck.Composition.Interfaces.MediaItemTypes.ViewModels;
using MediaDeck.Composition.Interfaces.Notifications;
using MediaDeck.Core.Models.Files;

namespace MediaDeck.ViewModels.Panes.ViewerPanes;

[Inject(InjectServiceLifetime.Scoped)]
public class MediaContentLibraryViewModel : ViewModelBase {
	public MediaContentLibraryViewModel(MediaContentLibrary mediaContentLibrary, SearchConditionManagerViewModel searchConditionManagerViewModel, IMediaItemTypeService MediaItemTypeService) {
		this.SearchConditionManagerViewModel = searchConditionManagerViewModel;
		this.Files = mediaContentLibrary.Files.CreateView(MediaItemTypeService.CreateMediaItemViewModel).ToNotifyCollectionChanged(SynchronizationContextCollectionEventDispatcher.Current);
		this.SearchElapsedMilliseconds = mediaContentLibrary.SearchElapsedMilliseconds.ObserveOnCurrentSynchronizationContext().ToBindableReactiveProperty().AddTo(this.CompositeDisposable);
	}

	public SearchConditionManagerViewModel SearchConditionManagerViewModel {
		get;
	}

	public NotifyCollectionChangedSynchronizedViewList<IMediaItemViewModel> Files {
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
}