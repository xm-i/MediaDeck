using MediaDeck.Composition.Interfaces.Files;

namespace MediaDeck.Core.Models.NotificationDispatcher;

[Inject(InjectServiceLifetime.Scoped)]
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