using MediaDeck.Common.Base;
using MediaDeck.Core.Services.FileChangeMonitor;

namespace MediaDeck.ViewModels;

[Inject(InjectServiceLifetime.Scoped)]
public class NavigationMenuViewModel : ViewModelBase {
	public BindableReactiveProperty<bool> HasUnprocessedChanges {
		get;
	}

	public NavigationMenuViewModel(FileChangeMonitorService fileChangeMonitorService) {
		this.HasUnprocessedChanges = fileChangeMonitorService.Tracker.UnprocessedChanges
			.ObserveCountChanged()
			.ObserveOnCurrentSynchronizationContext()
			.Select(count => count > 0)
			.ToBindableReactiveProperty(fileChangeMonitorService.Tracker.UnprocessedChanges.Count > 0)
			.AddTo(this.CompositeDisposable);
	}
}