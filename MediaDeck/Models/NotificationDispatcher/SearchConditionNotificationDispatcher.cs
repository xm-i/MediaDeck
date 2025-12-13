using MediaDeck.Composition.Interfaces.Files;

namespace MediaDeck.Models.NotificationDispatcher;
[Inject(InjectServiceLifetime.Singleton)]
public class SearchConditionNotificationDispatcher {
	public Subject<ISearchCondition> AddRequest {
		get;
	} = new();

	public Subject<ISearchCondition> RemoveRequest {
		get;
	} = new();

	public Subject<Action<ObservableList<ISearchCondition>>> UpdateRequest {
		get;
	} = new();
}
