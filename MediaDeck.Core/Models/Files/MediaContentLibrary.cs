using System.Diagnostics;
using MediaDeck.Common.Base;
using MediaDeck.Composition.Interfaces.Files;
using MediaDeck.Composition.Interfaces.FileTypes.Models;
using MediaDeck.Composition.Interfaces.Tags;
using MediaDeck.Composition.Stores.Config.Model;
using MediaDeck.Composition.Stores.State.Model;
using MediaDeck.Core.Models.Files.Loaders;
using MediaDeck.Core.Models.Files.SearchConditions;
using MediaDeck.Core.Models.NotificationDispatcher;
using MediaDeck.Core.Models.Repositories;

namespace MediaDeck.Core.Models.Files;

[Inject(InjectServiceLifetime.Scoped)]
public class MediaContentLibrary : ModelBase {
	public MediaContentLibrary(FilesLoader filesLoader, SearchConfigModel searchConfig, SearchConditionNotificationDispatcher searchConditionNotificationDispatcher, ITagsManager tagsManager, FolderRepository folderRepository, TabStateModel tabState) {
		this._filesLoader = filesLoader;
		this._searchConfig = searchConfig;
		this.SearchConditions.ObserveChanged().ThrottleLast(TimeSpan.FromMilliseconds(100)).SubscribeAwait(async (_, ct) => await this.SearchAsync(), AwaitOperation.Drop).AddTo(this.CompositeDisposable);
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
	private readonly SearchConfigModel _searchConfig;
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

	public ReactiveProperty<long?> SearchElapsedMilliseconds {
		get;
	} = new();

	public async Task SearchAsync() {
		if (this._searchCts is { } oldCts) {
			await oldCts.CancelAsync();
		}
		var cts = this._searchCts = new CancellationTokenSource();
		this.SearchElapsedMilliseconds.Value = null;
		try {
			var stopwatch = Stopwatch.StartNew();

			int initialLoadCount = this._searchConfig.InitialLoadCount.Value;
			int incrementalLoadCount = this._searchConfig.IncrementalLoadCount.Value;
			int maxLoadCount = this._searchConfig.MaxLoadCount.Value;

			var stream = this._filesLoader.GetFilesStreamAsync(this.SearchConditions, cts.Token);

			var batch = new List<IFileModel>();
			int totalLoaded = 0;
			bool isInitial = true;

			await foreach (var fileModel in stream) {
				cts.Token.ThrowIfCancellationRequested();

				batch.Add(fileModel);
				totalLoaded++;

				if (isInitial && batch.Count >= initialLoadCount) {
					this.Files.Clear();
					this.Files.AddRange(batch);
					batch.Clear();
					isInitial = false;
				} else if (!isInitial && batch.Count >= incrementalLoadCount) {
					this.Files.AddRange(batch);
					batch.Clear();
				}

				if (totalLoaded >= maxLoadCount) {
					break;
				}
			}

			if (batch.Count > 0) {
				if (isInitial) {
					this.Files.Clear();
				}
				this.Files.AddRange(batch);
			} else if (isInitial && totalLoaded == 0) {
				this.Files.Clear();
			}

			stopwatch.Stop();
			this.SearchElapsedMilliseconds.Value = stopwatch.ElapsedMilliseconds;
		} catch (OperationCanceledException) when (cts.Token.IsCancellationRequested) {
			// cancelled by a newer search
		}
	}
}