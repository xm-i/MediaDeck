using System.Diagnostics;

using MediaDeck.Common.Base;
using MediaDeck.Composition.Interfaces.Files;
using MediaDeck.Composition.Interfaces.FileTypes.Models;
using MediaDeck.Composition.Interfaces.Tags;
using MediaDeck.Composition.Stores.State.Model;
using MediaDeck.Core.Models.Files.Loaders;
using MediaDeck.Core.Models.Files.SearchConditions;
using MediaDeck.Core.Models.NotificationDispatcher;
using MediaDeck.Core.Models.Repositories;

namespace MediaDeck.Core.Models.Files;

[Inject(InjectServiceLifetime.Scoped)]
public class MediaContentLibrary : ModelBase {
	public MediaContentLibrary(FilesLoader filesLoader, SearchConditionNotificationDispatcher searchConditionNotificationDispatcher, ITagsManager tagsManager, FolderRepository folderRepository, TabStateModel tabState) {
		this._filesLoader = filesLoader;
		this.SearchConditions.ObserveChanged().ThrottleLast(TimeSpan.FromMilliseconds(100)).Subscribe(async _ => await this.SearchAsync()).AddTo(this.CompositeDisposable);
		this.SearchConditionCandidates.AddRange(tagsManager.Tags.Select(x => new TagSearchCondition(tagsManager) { TagId = x.TagId } as ISearchCondition));
		this.SearchConditionCandidates.AddRange(folderRepository.GetAllFolders().Select(x => new FolderSearchCondition { FolderPath = x.FolderPath } as ISearchCondition));
		searchConditionNotificationDispatcher.AddRequest.Subscribe(this.SearchConditions.Add).AddTo(this.CompositeDisposable);
		searchConditionNotificationDispatcher.RemoveRequest.Subscribe(x => this.SearchConditions.Remove(x)).AddTo(this.CompositeDisposable);
		searchConditionNotificationDispatcher.UpdateRequest.Subscribe(x => x(this.SearchConditions)).AddTo(this.CompositeDisposable);

		this.SearchConditions.AddRange(tabState.SearchState.SearchCondition.ToArray());
		this.SearchConditions.ObserveChanged()
			.Subscribe(_ => {
				tabState.SearchState.SearchCondition.Clear();
				tabState.SearchState.SearchCondition.AddRange(this.SearchConditions.ToArray());
			})
			.AddTo(this.CompositeDisposable);
	}

	private readonly FilesLoader _filesLoader;
	private CancellationTokenSource? _searchCts;

	public ObservableList<IFileModel> Files {
		get;
	} = [];

	public ObservableList<ISearchCondition> SearchConditions {
		get;
	} = [];

	public ObservableList<ISearchCondition> SearchConditionCandidates {
		get;
	} = [];

	public ReactiveProperty<long> SearchElapsedMilliseconds {
		get;
	} = new();

	public async Task SearchAsync() {
		if (this._searchCts is { } oldCts) {
			await oldCts.CancelAsync();
		}
		var cts = this._searchCts = new CancellationTokenSource();
		try {
			var stopwatch = Stopwatch.StartNew();
			var files = await this._filesLoader.Load(this.SearchConditions, cts.Token);
			cts.Token.ThrowIfCancellationRequested();
			stopwatch.Stop();
			this.Files.Clear();
			this.Files.AddRange(files);
			this.SearchElapsedMilliseconds.Value = stopwatch.ElapsedMilliseconds;
		} catch (OperationCanceledException) when (cts.Token.IsCancellationRequested) {
			// cancelled by a newer search
		}
	}
}