using CommunityToolkit.Mvvm.DependencyInjection;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using MediaDeck.ViewModels;
using MediaDeck.Views.FolderManager;
using MediaDeck.Views.Preferences;
using MediaDeck.Views.Tags;
using MediaDeck.Views.Tools;

namespace MediaDeck.Views;
public sealed partial class NavigationMenu : NavigationMenuUserControl {
	public NavigationMenu() {
		this.InitializeComponent();
		this.Loaded += this.NavigationMenu_Loaded;
	}

	private void NavigationMenu_Loaded(object sender, RoutedEventArgs e) {
		if(this.ViewModel == null) {
			return;
		}
		this.ViewModel.HasUnprocessedChanges.Subscribe(hasChanges => {
			this.DispatcherQueue?.TryEnqueue(() => {
				if (hasChanges) {
					this.NotificationPulseStoryboard.Begin();
				} else {
					this.NotificationPulseStoryboard.Stop();
				}
			});
		}).AddTo(this.ViewModel.CompositeDisposable);
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
			case "DuplicateDetector":
				window = Ioc.Default.GetRequiredService<DuplicateDetectorWindow>();
				break;
			case "Config":
				window = Ioc.Default.GetRequiredService<ConfigWindow>();
				break;
			case "BackgroundTasks":
				window = Ioc.Default.GetRequiredService<BackgroundTasksWindow>();
				break;
		}
		window?.ActivateCenteredOnMainWindow();
	}

	private void SyncNotificationButton_Click(object sender, RoutedEventArgs e) {
		var window = Ioc.Default.GetRequiredService<FileChangeSyncWindow>();
		window.ActivateCenteredOnMainWindow();
	}
}

public abstract class NavigationMenuUserControl : UserControlBase<NavigationMenuViewModel>;

