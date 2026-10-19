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
	private readonly IWindowService _windowService;

	public NavigationMenu() {
		this.InitializeComponent();
		this._windowService = Ioc.Default.GetRequiredService<IWindowService>();
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

		switch (selectedItem.Tag?.ToString()) {
			case "TagManager":
				this._windowService.ActivateCentered<TagManagerWindow>(this);
				break;
			case "FolderManager":
				this._windowService.ActivateCentered<FolderManagerWindow>(this);
				break;
			case "DuplicateDetector":
				this._windowService.ActivateCentered<DuplicateDetectorWindow>(this);
				break;
			case "Config":
				this._windowService.ActivateCentered<ConfigWindow>(this);
				break;
			case "About":
				this._windowService.ActivateCentered<AboutWindow>(this);
				break;
			case "License":
				this._windowService.ActivateCentered<LicenseWindow>(this);
				break;
		}
	}

	private void SyncNotificationButton_Click(object sender, RoutedEventArgs e) {
		this._windowService.ActivateCentered<FileChangeSyncWindow>(this);
	}
}


public abstract class NavigationMenuUserControl : UserControlBase<NavigationMenuViewModel>;