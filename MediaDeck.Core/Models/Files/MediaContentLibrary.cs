using System.Diagnostics;

using MediaDeck.Composition.Interfaces.Files;
using MediaDeck.Composition.Interfaces.FileTypes.Models;
using MediaDeck.Composition.Stores.State.Model;
using MediaDeck.Common.Base;
using MediaDeck.Core.Models.FileDetailManagers;
using MediaDeck.Core.Models.Files.Loaders;
using MediaDeck.Core.Models.Files.SearchConditions;
using MediaDeck.Core.Models.NotificationDispatcher;
using MediaDeck.Core.Models.Repositories;

namespace MediaDeck.Core.Models.Files;

[Inject(InjectServiceLifetime.Singleton)]
public class MediaContentLibrary : ModelBase {
	public MediaContentLibrary(FilesLoader filesLoader, SearchConditionNotificationDispatcher searchConditionNotificationDispatcher, TagsManager tagsManager, FolderRepository folderRepository, StateModel states) {
		this._filesLoader = filesLoader;
		this.SearchConditions.ObserveChanged().ThrottleLast(TimeSpan.FromMilliseconds(100)).Subscribe(async _ => await this.SearchAsync());
		tagsManager.Load().Wait();
		this.SearchConditionCandidates.AddRange(tagsManager.Tags.Select(x => new TagSearchCondition(x) as ISearchCondition));
		this.SearchConditionCandidates.AddRange(folderRepository.GetAllFolders().Select(x => new FolderSearchCondition(x) as ISearchCondition));
		searchConditionNotificationDispatcher.AddRequest.Subscribe(this.SearchConditions.Add);
		searchConditionNotificationDispatcher.RemoveRequest.Subscribe(x => this.SearchConditions.Remove(x));
		searchConditionNotificationDispatcher.UpdateRequest.Subscribe(x => x(this.SearchConditions));

		this.SearchConditions.AddRange(states.SearchState.SearchCondition.ToArray());
		this.SearchConditions.ObserveChanged()
			.Subscribe(_ => {
				states.SearchState.SearchCondition.Clear();
				states.SearchState.SearchCondition.AddRange(this.SearchConditions.ToArray());
			});
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