using MediaDeck.Common.Base;
using MediaDeck.Composition.Interfaces.Files;
using MediaDeck.Composition.Interfaces.MediaItemTypes;
using MediaDeck.Composition.Interfaces.MediaItemTypes.ViewModels;
using MediaDeck.Composition.Interfaces.Notifications;
using MediaDeck.Core.Models.Files;

namespace MediaDeck.ViewModels.Panes.ViewerPanes;

[Inject(InjectServiceLifetime.Scoped)]
public class MediaContentLibraryViewModel : ViewModelBase {
	public MediaContentLibraryViewModel(MediaContentLibrary mediaContentLibrary, ISearchConditionNotificationDispatcher searchConditionNotificationDispatcher, IMediaItemTypeService MediaItemTypeService) {
		this._mediaContentLibrary = mediaContentLibrary;
		this.Files = mediaContentLibrary.Files.CreateView(MediaItemTypeService.CreateMediaItemViewModel).ToNotifyCollectionChanged(SynchronizationContextCollectionEventDispatcher.Current);

		this.SearchConditions =
			mediaContentLibrary
				.SearchConditions
				.ToWritableNotifyCollectionChanged(x => new SearchConditionViewModel(x).AddTo(this.CompositeDisposable),
					(SearchConditionViewModel scvm, ISearchCondition sc, ref bool setValue) => scvm.SearchCondition,
					SynchronizationContextCollectionEventDispatcher.Current);

		this.SearchConditionCandidates = this._mediaContentLibrary.SearchConditionCandidates.CreateView(x => new SearchConditionViewModel(x).AddTo(this.CompositeDisposable));
		this.FilteredSearchConditionCandidates = this.SearchConditionCandidates.ToNotifyCollectionChanged(SynchronizationContextCollectionEventDispatcher.Current);
		this.SearchElapsedMilliseconds = mediaContentLibrary.SearchElapsedMilliseconds.ObserveOnCurrentSynchronizationContext().ToBindableReactiveProperty().AddTo(this.CompositeDisposable);

		this.SearchConditionNotificationDispatcher = searchConditionNotificationDispatcher;
	}

	private readonly MediaContentLibrary _mediaContentLibrary;

	public NotifyCollectionChangedSynchronizedViewList<IMediaItemViewModel> Files {
		get;
	}

	public INotifyCollectionChangedSynchronizedViewList<SearchConditionViewModel> SearchConditions {
		get;
	}

	public ISynchronizedView<ISearchCondition, SearchConditionViewModel> SearchConditionCandidates {
		get;
	}

	public INotifyCollectionChangedSynchronizedViewList<SearchConditionViewModel> FilteredSearchConditionCandidates {
		get;
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
		get;
	}

	public void RefreshSearchTokenCandidates(string word) {
		this.SearchConditionCandidates.AttachFilter(x => {
			return x.IsMatchForSuggest(word);
		});
	}

	/// <summary>手動リロードを実行する（Dispatcher 経由で検索を発火する）。</summary>
	public void Reload() {
		this.SearchConditionNotificationDispatcher.ReloadRequested.OnNext(Unit.Default);
	}
}