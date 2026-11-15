using CommunityToolkit.Mvvm.DependencyInjection;
using MediaDeck.Composition.Interfaces;
using MediaDeck.Composition.Interfaces.Services;
using MediaDeck.Core.Stores.Config;
using MediaDeck.ViewModels.FolderManager;
using MediaDeck.Views.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace MediaDeck.Views.FolderManager;

/// <summary>
/// フォルダ一覧の管理（登録・削除・スキャン）を行うビュー。
/// </summary>
public sealed partial class FolderManagerView {
	/// <summary>ダイアログ表示用親ウィンドウを取得または設定します。</summary>
	public Window? ParentWindow {
		get;
		set;
	}

	/// <summary>
	/// <see cref="FolderManagerView"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	public FolderManagerView() {
		this.InitializeComponent();
	}

	private async void AddFolderButton_Click(object sender, RoutedEventArgs e) {
		if (this.ViewModel is null) {
			return;
		}
		var filePickerService = Ioc.Default.GetRequiredService<IFilePickerService>();
		var folderPath = await filePickerService.PickFolderAsync(this.ParentWindow);
		if (string.IsNullOrWhiteSpace(folderPath)) {
			return;
		}
		this.ViewModel.AddFolderCommand.Execute(folderPath);
	}

	private async void RemoveFolderButton_Click(object sender, RoutedEventArgs e) {
		if (this.ViewModel?.SelectedFolder.Value is not FolderViewModel target) {
			return;
		}

		var stringProvider = Ioc.Default.GetRequiredService<IStringProvider>();
		var dialog = new ContentDialog {
			XamlRoot = this.Content.XamlRoot,
			Title = stringProvider.GetString("FolderManager_DeleteConfirmation_Title"),
			PrimaryButtonText = stringProvider.GetString("DialogButton_Delete"),
			CloseButtonText = stringProvider.GetString("DialogButton_Cancel"),
			DefaultButton = ContentDialogButton.Primary
		};
		using var disposable = new CompositeDisposable();
		ThemeHelper.BindTheme(dialog, Ioc.Default.GetRequiredService<IConfigStore>(), disposable);

		var result = await dialog.ShowAsync();

		if (result != ContentDialogResult.Primary) {
			return;
		}
		this.ViewModel.RemoveFolderCommand.Execute(target);
	}
}

/// <summary>
/// フォルダマネージャ View の基底コントロール。
/// </summary>
public abstract class FolderManagerViewUserControl : UserControlBase<FolderManagerViewModel>;