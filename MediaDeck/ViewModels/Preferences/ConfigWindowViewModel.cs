using MediaDeck.Composition.Bases;
using MediaDeck.Stores.Config;
using MediaDeck.ViewModels.Preferences.Config;

namespace MediaDeck.ViewModels.Preferences;

[Inject(InjectServiceLifetime.Transient)]
public class ConfigWindowViewModel : ViewModelBase {
	public ConfigWindowViewModel(ConfigStore configStore,ScanConfigPageViewModel scanConfigPageViewModel,ExecutionConfigPageViewModel executionConfigPageViewModel) {
		this.ScanConfigPageViewModel = scanConfigPageViewModel;
		this.ExecutionConfigPageViewModel = executionConfigPageViewModel;
		this.ConfigPageViewModels = [
			this.ScanConfigPageViewModel,
			this.ExecutionConfigPageViewModel
		];
		this.SelectedPageViewModel.Value = this.ScanConfigPageViewModel;
		this.SaveCommand.Subscribe(_ => {
			configStore.Save();
		});

		this.LoadCommand.Subscribe(_ => {
			configStore.Load();
		});
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
