using CommunityToolkit.Mvvm.DependencyInjection;
using MediaDeck.Core.Stores.State;
using MediaDeck.ViewModels.FolderManager;
using MediaDeck.Views.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace MediaDeck.Views.FolderManager;

public sealed partial class FolderManagerView {
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

		var hWnd = WindowNative.GetWindowHandle(this.ParentWindow);
		InitializeWithWindow.Initialize(openPicker, hWnd);
		openPicker.SuggestedStartLocation = PickerLocationId.Desktop;
		openPicker.FileTypeFilter.Add("*");

		var folder = await openPicker.PickSingleFolderAsync();
		if (folder == null) {
			return;
		}
		this.ViewModel.AddFolderCommand.Execute(folder.Path);
	}

	private async void RemoveFolderButton_Click(object sender, RoutedEventArgs e) {
		if (this.ViewModel?.SelectedFolder.Value is not FolderViewModel target) {
			return;
		}

		var dialog = new ContentDialog {
			XamlRoot = this.Content.XamlRoot,
			Title = "このフォルダを削除しますか？この操作は元に戻せません。",
			PrimaryButtonText = "削除",
			CloseButtonText = "キャンセル",
			DefaultButton = ContentDialogButton.Primary
		};
		using var disposable = new CompositeDisposable();
		ThemeHelper.BindTheme(dialog, Ioc.Default.GetRequiredService<IStateStore>(), disposable);

		var result = await dialog.ShowAsync();

		if (result != ContentDialogResult.Primary) {
			return;
		}
		this.ViewModel.RemoveFolderCommand.Execute(target);
	}
}

public abstract class FolderManagerViewUserControl : UserControlBase<FolderManagerViewModel>;