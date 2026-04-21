using MediaDeck.Common.Base;
using MediaDeck.Composition.Interfaces.Files;
using MediaDeck.Composition.Interfaces.FileTypes;
using MediaDeck.Composition.Interfaces.FileTypes.ViewModels;
using MediaDeck.Core.Models.Files;
using MediaDeck.Core.Models.NotificationDispatcher;

namespace MediaDeck.ViewModels.Panes.ViewerPanes;

[Inject(InjectServiceLifetime.Scoped)]
public class MediaContentLibraryViewModel : ViewModelBase {
	public MediaContentLibraryViewModel(MediaContentLibrary mediaContentLibrary, SearchConditionNotificationDispatcher searchConditionNotificationDispatcher, IFileTypeService fileTypeService) {
		this._mediaContentLibrary = mediaContentLibrary;
		this.Files = mediaContentLibrary.Files.CreateView(fileTypeService.CreateFileViewModel).ToNotifyCollectionChanged(SynchronizationContextCollectionEventDispatcher.Current);

		this.SearchConditions =
			mediaContentLibrary
				.SearchConditions
				.ToWritableNotifyCollectionChanged(x => new SearchConditionViewModel(x),
					(SearchConditionViewModel scvm, ISearchCondition sc, ref bool setValue) => scvm.SearchCondition,
					SynchronizationContextCollectionEventDispatcher.Current);

		this.SearchConditionCandidates = this._mediaContentLibrary.SearchConditionCandidates.CreateView(x => new SearchConditionViewModel(x));
		this.FilteredSearchConditionCandidates = this.SearchConditionCandidates.ToNotifyCollectionChanged(SynchronizationContextCollectionEventDispatcher.Current);
		this.SearchElapsedMilliseconds = mediaContentLibrary.SearchElapsedMilliseconds.ObserveOnCurrentSynchronizationContext().ToBindableReactiveProperty().AddTo(this.CompositeDisposable);

		this.SearchConditionNotificationDispatcher = searchConditionNotificationDispatcher;
	}

	private readonly MediaContentLibrary _mediaContentLibrary;

	public NotifyCollectionChangedSynchronizedViewList<IFileViewModel> Files {
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

	public BindableReactiveProperty<IFileViewModel> SelectedFile {
		get;
	} = new();

	public BindableReactiveProperty<IFileViewModel[]> SelectedFiles {
		get;
	} = new();

	public BindableReactiveProperty<long?> SearchElapsedMilliseconds {
		get;
	}

	public SearchConditionNotificationDispatcher SearchConditionNotificationDispatcher {
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