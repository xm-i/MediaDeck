using CommunityToolkit.Mvvm.DependencyInjection;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using MediaDeck.Composition.Bases;
using MediaDeck.ViewModels;
using MediaDeck.Views.FolderManager;
using MediaDeck.Views.Preferences;
using MediaDeck.Views.Tags;
using MediaDeck.Views.Tools;

namespace MediaDeck.Views;
public sealed partial class NavigationMenu : NavigationMenuUserControl {
	public NavigationMenu() {
		this.InitializeComponent();
	}

	private void MenuFlyoutItem_Click(object sender, RoutedEventArgs e) {
		if (sender is not MenuFlyoutItem selectedItem) {
			return;
		}
		Window? window = null;
		switch (selectedItem.Tag.ToString()) {
			case "TagManager":
				window = Ioc.Default.GetRequiredService<TagManagerWindow>();
				break;
			case "FolderManager":
				window = Ioc.Default.GetRequiredService<FolderManagerWindow>();
				break;
			case "Config":
				window = Ioc.Default.GetRequiredService<ConfigWindow>();
				break;
			case "BackgroundTasks":
				window = Ioc.Default.GetRequiredService<BackgroundTasksWindow>();
				break;
		}
		window?.Activate();
	}
}

public abstract class NavigationMenuUserControl : UserControlBase<NavigationMenuViewModel>;

