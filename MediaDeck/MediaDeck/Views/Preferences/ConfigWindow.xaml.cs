using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using MediaDeck.ViewModels.Preferences;
using MediaDeck.Views.Preferences.CustomConfig;

using Windows.Graphics;

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
	}

	private void NavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args) {
		if (args.SelectedItem is not NavigationViewItem selectedItem) {
			return;
		}

		switch(selectedItem.Tag) {
			case "ScanConfig":
				this.ContentFrame.Navigate(typeof(ScanConfigPage));
				break;
			case "ExecutionConfig":
				this.ContentFrame.Navigate(typeof(ExecutionConfigPage));
				break;
		}
	}
}
