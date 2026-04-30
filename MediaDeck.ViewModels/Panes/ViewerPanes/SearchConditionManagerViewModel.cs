using MediaDeck.Common.Base;
using MediaDeck.Composition.Interfaces.Files;
using MediaDeck.Composition.Interfaces.Notifications;
using MediaDeck.Core.Models.Files;

namespace MediaDeck.ViewModels.Panes.ViewerPanes;

[Inject(InjectServiceLifetime.Scoped)]
public class SearchConditionManagerViewModel : ViewModelBase {
	public SearchConditionManagerViewModel(MediaContentLibrary mediaContentLibrary, ISearchConditionNotificationDispatcher searchConditionNotificationDispatcher) {
		this.SearchConditions =
			mediaContentLibrary
				.SearchConditions
				.ToWritableNotifyCollectionChanged(x => new SearchConditionViewModel(x).AddTo(this.CompositeDisposable),
					(SearchConditionViewModel scvm, ISearchCondition sc, ref bool setValue) => scvm.SearchCondition,
					SynchronizationContextCollectionEventDispatcher.Current);

		this.SearchConditionCandidates = mediaContentLibrary.SearchConditionCandidates.CreateView(x => new SearchConditionViewModel(x).AddTo(this.CompositeDisposable));
		this.FilteredSearchConditionCandidates = this.SearchConditionCandidates.ToNotifyCollectionChanged(SynchronizationContextCollectionEventDispatcher.Current);
		this.SearchConditionNotificationDispatcher = searchConditionNotificationDispatcher;
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