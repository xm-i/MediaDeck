using Microsoft.UI.Xaml;

using MediaDeck.ViewModels.Preferences;
using MediaDeck.Views.Preferences.Config;

using Windows.Graphics;
using MediaDeck.ViewModels.Preferences.Config;

namespace MediaDeck.Views.Preferences;

[AddTransient]
public sealed partial class ConfigWindow : Window {
	public ConfigWindowViewModel ViewModel {
		get;
	}
	public ConfigWindow(ConfigWindowViewModel ConfigWindowViewModel) {
		this.InitializeComponent();
		this.ViewModel = ConfigWindowViewModel;
		this.ViewModel.LoadCommand.Execute(Unit.Default);
		this.AppWindow.Resize(new SizeInt32(1000, 700));
		this.ViewModel.SelectedPageViewModel.Subscribe(vm => {
			if (vm is null) {
				return;
			}
			Type view;
			switch (vm) {
				case ScanConfigPageViewModel _:
					view = typeof(ScanConfigPage);
					break;
				case ExecutionConfigPageViewModel _:
					view = typeof(ExecutionConfigPage);
					break;
				default:
					return;
			}

			this.ContentFrame.Navigate(view, vm);
		});
	}
}
