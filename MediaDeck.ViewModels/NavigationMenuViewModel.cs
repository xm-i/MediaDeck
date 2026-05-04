using MediaDeck.Common.Base;
using MediaDeck.Common.Extensions;
using MediaDeck.Composition.Enum;
using MediaDeck.Core.Services.FileChangeMonitor;
using MediaDeck.Core.Stores.State;

namespace MediaDeck.ViewModels;

[Inject(InjectServiceLifetime.Scoped)]
public class NavigationMenuViewModel : ViewModelBase {
	public BindableReactiveProperty<bool> HasUnprocessedChanges {
		get;
	}

	/// <summary>
	/// 現在のテーマ
	/// </summary>
	public BindableReactiveProperty<AppTheme> CurrentTheme {
		get;
	}

	/// <summary>
	/// テーマを設定するコマンド
	/// </summary>
	public ReactiveCommand<AppTheme> SetThemeCommand {
		get;
	} = new();

	public NavigationMenuViewModel(FileChangeMonitorService fileChangeMonitorService, IStateStore stateStore) {
		this.HasUnprocessedChanges = fileChangeMonitorService.Tracker.UnprocessedChanges
			.ObserveCountChanged()
			.ObserveOnCurrentSynchronizationContext()
			.Select(count => count > 0)
			.ToBindableReactiveProperty(fileChangeMonitorService.Tracker.UnprocessedChanges.Count > 0)
			.AddTo(this.CompositeDisposable);

		this.CurrentTheme = stateStore.RootState.AppState.Theme
			.ToTwoWayBindableReactiveProperty()
			.AddTo(this.CompositeDisposable);

		this.SetThemeCommand.Subscribe(theme => {
			stateStore.RootState.AppState.Theme.Value = theme;
		}).AddTo(this.CompositeDisposable);
	}
}