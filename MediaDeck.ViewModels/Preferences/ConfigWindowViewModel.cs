using MediaDeck.Common.Base;
using MediaDeck.Core.Stores.Config;
using MediaDeck.ViewModels.Preferences.Config;

namespace MediaDeck.ViewModels.Preferences;

[Inject(InjectServiceLifetime.Transient)]
public class ConfigWindowViewModel : ViewModelBase {
	public ConfigWindowViewModel(IConfigStore configStore, ScanConfigPageViewModel scanConfigPageViewModel, ExecutionConfigPageViewModel executionConfigPageViewModel) {
		this.ScanConfigPageViewModel = scanConfigPageViewModel;
		this.ExecutionConfigPageViewModel = executionConfigPageViewModel;
		this.ConfigPageViewModels = [
			this.ScanConfigPageViewModel,
			this.ExecutionConfigPageViewModel
		];
		this.SelectedPageViewModel.Value = this.ScanConfigPageViewModel;
		this.SaveCommand.Subscribe(_ => {
			configStore.Save();
		}).AddTo(this.CompositeDisposable);

		this.LoadCommand.Subscribe(_ => {
			configStore.Load();
		}).AddTo(this.CompositeDisposable);
	}

	public IConfigPageViewModel[] ConfigPageViewModels {
		get;
	}

	public BindableReactiveProperty<IConfigPageViewModel> SelectedPageViewModel {
		get;
	} = new();

	public ReactiveCommand SaveCommand {
		get;
	} = new();

	public ReactiveCommand LoadCommand {
		get;
	} = new();

	public ScanConfigPageViewModel ScanConfigPageViewModel {
		get;
	}

	public ExecutionConfigPageViewModel ExecutionConfigPageViewModel {
		get;
	}
}