using CommunityToolkit.Mvvm.DependencyInjection;
using MediaDeck.Services;
using MediaDeck.ViewModels;
using MediaDeck.Views.FolderManager;
using MediaDeck.Views.Help;
using MediaDeck.Views.Preferences;
using MediaDeck.Views.Tags;
using MediaDeck.Views.Tools;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace MediaDeck.Views;

public sealed partial class NavigationMenu {
	private readonly WindowService _windowService;
	private readonly WindowManager _windowManager;

	public NavigationMenu() {
		this.InitializeComponent();
		this._windowService = Ioc.Default.GetRequiredService<WindowService>();
		this._windowManager = Ioc.Default.GetRequiredService<WindowManager>();
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

		if (selectedItem.Tag?.ToString() == "Exit") {
			Application.Current.Exit();
			return;
		}

		Window? window = selectedItem.Tag?.ToString() switch {
			"TagManager" => Ioc.Default.GetRequiredService<TagManagerWindow>(),
			"FolderManager" => Ioc.Default.GetRequiredService<FolderManagerWindow>(),
			"DuplicateDetector" => Ioc.Default.GetRequiredService<DuplicateDetectorWindow>(),
			"Config" => Ioc.Default.GetRequiredService<ConfigWindow>(),
			"About" => Ioc.Default.GetRequiredService<AboutWindow>(),
			"License" => Ioc.Default.GetRequiredService<LicenseWindow>(),
			_ => null
		};

		if (window != null) {
			var parent = this._windowManager.GetWindowFromElement(this);
			if (parent == null) {
				// TODO: notify
				return;
			}
			this._windowService.ActivateCenteredOnMainWindow(window, parent);
		}
	}

	private void SyncNotificationButton_Click(object sender, RoutedEventArgs e) {
		var window = Ioc.Default.GetRequiredService<FileChangeSyncWindow>();
		var parent = this._windowManager.GetWindowFromElement(this);
		if (parent == null) {
			// TODO: notify
			return;
		}
		this._windowService.ActivateCenteredOnMainWindow(window, parent);
	}
}


public abstract class NavigationMenuUserControl : UserControlBase<NavigationMenuViewModel>;