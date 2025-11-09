using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using MediaDeck.Composition.Bases;
using MediaDeck.ViewModels.FolderManager;

using Windows.Storage.Pickers;

namespace MediaDeck.Views.FolderManager;
public sealed partial class FolderManagerView : FolderManagerViewUserControl {
	public Window? ParentWindow {
		get;
		set;
	}

	public FolderManagerView() {
		this.InitializeComponent();
	}
	private async void AddFolderButton_Click(object sender, RoutedEventArgs e) {
		if (this.ViewModel is null) {
			return;
		}
		var openPicker = new FolderPicker();

		var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this.ParentWindow);
		WinRT.Interop.InitializeWithWindow.Initialize(openPicker, hWnd);
		openPicker.SuggestedStartLocation = PickerLocationId.Desktop;
		openPicker.FileTypeFilter.Add("*");

		var folder = await openPicker.PickSingleFolderAsync();
		if (folder == null) {
			return;
		}
		this.ViewModel.AddFolderCommand.Execute(folder.Path);
	}

	private async void MenuFlyoutItem_Click(object sender, RoutedEventArgs e) {
		if (this.ViewModel is null) {
			return;
		}
		if (sender is not MenuFlyoutItem selectedItem) {
			return;
		}
		switch (selectedItem.Tag.ToString()) {
			case "Remove":
				if (selectedItem.DataContext is not FolderViewModel target) {
					return;
				}

				var dialog = new ContentDialog {
					XamlRoot = this.Content.XamlRoot,
					Title = "Are you sure you want to delete this folder? This action cannot be undone.",
					PrimaryButtonText = "Remove",
					CloseButtonText = "Cancel",
					DefaultButton = ContentDialogButton.Primary
				};

				var result = await dialog.ShowAsync();

				if (result != ContentDialogResult.Primary) {
					return;
				}
				this.ViewModel.RemoveFolderCommand.Execute(target);
				break;
		}
	}
}

public abstract class FolderManagerViewUserControl : UserControlBase<FolderManagerViewModel>;


