using MediaDeck.Composition.Bases;
using MediaDeck.Models.Services;

namespace MediaDeck.ViewModels;

[Inject(InjectServiceLifetime.Transient)]
public class NavigationMenuViewModel : ViewModelBase {
	public BindableReactiveProperty<bool> HasUnprocessedChanges {
		get;
	}

	public NavigationMenuViewModel(FileChangeMonitorService fileChangeMonitorService) {
		this.HasUnprocessedChanges = fileChangeMonitorService.Tracker.UnprocessedChanges
			.ObserveCountChanged()
			.Select(count => count > 0)
			.ToBindableReactiveProperty(fileChangeMonitorService.Tracker.UnprocessedChanges.Count > 0)
			.AddTo(this.CompositeDisposable);
	}
}
