using System.Reactive.Linq;
using System.Threading.Tasks;

using MediaDeck.Composition.Bases;
using MediaDeck.FileTypes.Base.Models.Interfaces;
using MediaDeck.Models.FileDetailManagers;
using MediaDeck.Models.Files.Loaders;
using MediaDeck.Models.Files.SearchConditions;
using MediaDeck.Models.NotificationDispatcher;
using MediaDeck.Models.Preferences;
using MediaDeck.Models.Repositories;

namespace MediaDeck.Models.Files;

[AddSingleton]
public class MediaContentLibrary: ModelBase {
	public MediaContentLibrary(FilesLoader filesLoader, SearchConditionNotificationDispatcher searchConditionNotificationDispatcher,TagsManager tagsManager, FolderRepository folderRepository, States states) {
		this._filesLoader = filesLoader;
		this.SearchConditions.ObserveChanged().ThrottleLast(TimeSpan.FromMilliseconds(100)).Subscribe(async _ => await this.SearchAsync());
		tagsManager.Load().Wait();
		this.SearchConditionCandidates.AddRange(tagsManager.Tags.Select(x => new TagSearchCondition(x) as ISearchCondition));
		this.SearchConditionCandidates.AddRange(folderRepository.GetAllFolders().Select(x => new FolderSearchCondition(x) as ISearchCondition));
		searchConditionNotificationDispatcher.AddRequest.Subscribe(this.SearchConditions.Add);
		searchConditionNotificationDispatcher.RemoveRequest.Subscribe(x => this.SearchConditions.Remove(x));
		searchConditionNotificationDispatcher.UpdateRequest.Subscribe(x => x(this.SearchConditions));

		this.SearchConditions.AddRange(states.SearchStates.SearchCondition.ToArray());
		this.SearchConditions.ObserveChanged().Subscribe(_ => {
			states.SearchStates.SearchCondition.Clear();
			states.SearchStates.SearchCondition.AddRange(this.SearchConditions.ToArray());
		});
	}
	private readonly FilesLoader _filesLoader;

	public ObservableList<IFileModel> Files {
		get;
	} = [];

	public ObservableList<ISearchCondition> SearchConditions {
		get;
	} = [];

	public ObservableList<ISearchCondition> SearchConditionCandidates {
		get;
	} = [];

	public async Task SearchAsync() {
		var files = await this._filesLoader.Load(this.SearchConditions);
		this.Files.Clear();
		this.Files.AddRange(files);
	}
}
