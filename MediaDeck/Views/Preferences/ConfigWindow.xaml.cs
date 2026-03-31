using MediaDeck.ViewModels.Preferences;
using MediaDeck.ViewModels.Preferences.Config;
using MediaDeck.Views.Preferences.Config;

namespace MediaDeck.Views.Preferences;

[Inject(InjectServiceLifetime.Transient)]
public sealed partial class ConfigWindow {
	public ConfigWindowViewModel ViewModel {
		get;
	}

	public ConfigWindow(ConfigWindowViewModel configWindowViewModel) {
		this.InitializeComponent();
		this.ViewModel = configWindowViewModel;
		this.ViewModel.LoadCommand.Execute(Unit.Default);
		this.AppWindow.Resize(new(1000, 700));
		this.ViewModel.SelectedPageViewModel.Subscribe(vm => {
			Type view;
			switch (vm) {
				case ScanConfigPageViewModel:
					view = typeof(ScanConfigPage);
					break;
				case ExecutionConfigPageViewModel:
					view = typeof(ExecutionConfigPage);
					break;
				default:
					return;
			}

			this.ContentFrame.Navigate(view, vm);
		});
	}
}