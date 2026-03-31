using CommunityToolkit.Mvvm.DependencyInjection;

using MediaDeck.ViewModels;
using MediaDeck.Views.FolderManager;
using MediaDeck.Views.Preferences;
using MediaDeck.Views.Tags;
using MediaDeck.Views.Tools;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace MediaDeck.Views;

public sealed partial class NavigationMenu {
	public NavigationMenu() {
		this.InitializeComponent();
		this.Loaded += this.NavigationMenu_Loaded;
	}

	private void NavigationMenu_Loaded(object sender, RoutedEventArgs e) {
		if (this.ViewModel == null) {
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
			})
			.AddTo(this.ViewModel.CompositeDisposable);
	}

	private void MenuFlyoutItem_Click(object sender, RoutedEventArgs e) {
		if (sender is not MenuFlyoutItem selectedItem) {
			return;
		}
		Window? window = selectedItem.Tag.ToString() switch {
			"TagManager" => Ioc.Default.GetRequiredService<TagManagerWindow>(),
			"FolderManager" => Ioc.Default.GetRequiredService<FolderManagerWindow>(),
			"DuplicateDetector" => Ioc.Default.GetRequiredService<DuplicateDetectorWindow>(),
			"Config" => Ioc.Default.GetRequiredService<ConfigWindow>(),
			"BackgroundTasks" => Ioc.Default.GetRequiredService<BackgroundTasksWindow>(),
			_ => null
		};
		window?.ActivateCenteredOnMainWindow();
	}

	private void SyncNotificationButton_Click(object sender, RoutedEventArgs e) {
		var window = Ioc.Default.GetRequiredService<FileChangeSyncWindow>();
		window.ActivateCenteredOnMainWindow();
	}
}

public abstract class NavigationMenuUserControl : UserControlBase<NavigationMenuViewModel>;