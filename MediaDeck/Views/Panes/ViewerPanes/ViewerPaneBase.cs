using System.Threading.Tasks;
using CommunityToolkit.Mvvm.DependencyInjection;
using MediaDeck.Composition.Interfaces;
using MediaDeck.Composition.Interfaces.MediaItemTypes.ViewModels;
using MediaDeck.Core.Stores.Config;
using MediaDeck.Services;
using MediaDeck.ViewModels.Panes.ViewerPanes;
using MediaDeck.Views.Dialogs;
using MediaDeck.Views.Helpers;
using MediaDeck.Views.Thumbnails;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.System;
using Windows.UI.Core;

namespace MediaDeck.Views.Panes.ViewerPanes;

/// <summary>
/// 各種メディアビューアペインの基底クラス。
/// 選択変更、ダブルタップ実行、コンテキストメニュー、Ctrl+ホイールによるズームなどの共通UI動作を提供します。
/// </summary>
/// <typeparam name="T">ビューアの ViewModel 型。</typeparam>
public abstract class ViewerPaneBase<T> : UserControlBase<T> where T : ViewerPaneViewModelBase {
	private const string AddToAlbumRootTag = "AddToAlbumRoot";
	private const string AddToAlbumItemTagPrefix = "AddToAlbum:";
	private const string AddToAlbumCreateTag = "AddToAlbum:Create";
	private readonly IWindowService _windowService;
	private readonly IStringProvider _stringProvider;
	private IMediaItemViewModel? _contextMenuTargetFile;

	/// <summary>
	/// <see cref="ViewerPaneBase{T}"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	protected ViewerPaneBase() {
		this._windowService = Ioc.Default.GetRequiredService<IWindowService>();
		this._stringProvider = Ioc.Default.GetRequiredService<IStringProvider>();
	}

	/// <summary>
	/// リストコントロールの選択項目変更ハンドラ。
	/// </summary>
	/// <param name="sender">イベント発生元。</param>
	/// <param name="e">イベント引数。</param>
	protected virtual void List_SelectionChanged(object sender, SelectionChangedEventArgs e) {
		if (this.ViewModel is not { } vm) {
			return;
		}
		if (sender is ListViewBase listViewBase) {
			vm.MediaContentLibraryViewModel.SelectedFiles.Value = listViewBase.SelectedItems.Select(x => x as IMediaItemViewModel).Where(x => x is { }).ToArray()!;
		} else if (sender is ListBox listBox) {
			vm.MediaContentLibraryViewModel.SelectedFiles.Value = listBox.SelectedItems.Select(x => x as IMediaItemViewModel).Where(x => x is { }).ToArray()!;
		}
	}

	/// <summary>
	/// ファイルアイテムのダブルタップハンドラ。既定のプログラムまたは関連付けで実行します。
	/// </summary>
	/// <param name="sender">イベント発生元。</param>
	/// <param name="e">イベント引数。</param>
	protected async void File_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e) {
		if (sender is not Grid grid) {
			return;
		}
		if (grid.DataContext is not IMediaItemViewModel fileViewModel) {
			return;
		}
		await fileViewModel.ExecuteFileAsync();
	}

	/// <summary>
	/// ファイルアイテムの右タップハンドラ。コンテキストメニューを表示します。
	/// </summary>
	/// <param name="sender">イベント発生元。</param>
	/// <param name="e">イベント引数。</param>
	protected void List_RightTapped(object sender, RightTappedRoutedEventArgs e) {
		if (sender is not FrameworkElement parentControl) {
			return;
		}

		var element = e.OriginalSource as FrameworkElement;
		while (element != null && element.DataContext is not IMediaItemViewModel) {
			element = element.Parent as FrameworkElement;
		}

		if (element?.DataContext is not IMediaItemViewModel fileViewModel) {
			return;
		}
		this._contextMenuTargetFile = fileViewModel;

		if (parentControl.Resources["FileContextMenu"] is not MenuFlyout menuFlyout) {
			return;
		}

		menuFlyout.ShowAt(element, e.GetPosition(element));
	}

	/// <summary>
	/// コンテキストメニュー項目のクリックハンドラ。
	/// </summary>
	/// <param name="sender">イベント発生元。</param>
	/// <param name="e">イベント引数。</param>
	protected async void MenuFlyoutItem_Click(object sender, RoutedEventArgs e) {
		if (this.ViewModel is null) {
			return;
		}
		if (sender is not MenuFlyoutItem selectedItem) {
			return;
		}

		var fvm = this._contextMenuTargetFile;
		if (fvm is null) {
			return;
		}

		var selectedFiles = this.ViewModel.MediaContentLibraryViewModel.SelectedFiles.Value;
		var targetFiles = selectedFiles is { Length: > 0 } && selectedFiles.Contains(fvm) ? selectedFiles : [fvm];
		var tag = selectedItem.Tag?.ToString();

		if (tag == AddToAlbumCreateTag) {
			await this.CreateAlbumAndAddItemsAsync(targetFiles);
			return;
		}
		if (tag is { } t && t.StartsWith(AddToAlbumItemTagPrefix, StringComparison.Ordinal) && t != AddToAlbumCreateTag) {
			var albumPath = t[AddToAlbumItemTagPrefix.Length..];
			if (!string.IsNullOrWhiteSpace(albumPath)) {
				await this.ViewModel.AddToAlbumAsync(albumPath, targetFiles);
			}
			return;
		}

		switch (tag) {
			case "RecreateThumbnail":
				if (targetFiles.Length > 1) {
					this._windowService.ActivateCentered<BulkThumbnailRegenerationWindow>(this, bulkWindow => {
						bulkWindow.ViewModel.Initialize(targetFiles);
					});
				} else {
					this._windowService.ActivateCentered<ThumbnailPickerWindow>(this, window => {
						window.ViewModel.FileViewModel.Value = fvm;
					});
				}
				break;
			case "RemoveFile": {
					var message = targetFiles.Length == 1
						? this._stringProvider.GetString("Viewer_ContextRemoveFileSingle")
						: this._stringProvider.GetString("Viewer_ContextRemoveFileMultiple", targetFiles.Length);

					var dialog = new ContentDialog {
						XamlRoot = this.XamlRoot,
						Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
						Title = message,
						PrimaryButtonText = this._stringProvider.GetString("DialogButton_Yes"),
						SecondaryButtonText = this._stringProvider.GetString("DialogButton_No"),
						CloseButtonText = this._stringProvider.GetString("DialogButton_Cancel"),
						DefaultButton = ContentDialogButton.Primary
					};
					using var disposable = new CompositeDisposable();
					ThemeHelper.BindTheme(dialog, Ioc.Default.GetRequiredService<IConfigStore>(), disposable);

					var result = await dialog.ShowAsync();
					if (result == ContentDialogResult.Primary) {
						await this.ViewModel.DeleteFilesAndReloadAsync(targetFiles);
					}
					break;
				}
			case "UpdateMetadata":
				this.ViewModel.UpdateMetadata(targetFiles);
				break;
			case "OpenFolder":
				this.ViewModel.ShowInExplorer(fvm);
				break;
		}
	}

	/// <summary>
	/// ファイルコンテキストメニュー展開前の処理。動的な外部プログラム項目などを構築します。
	/// </summary>
	/// <param name="sender">イベント発生元。</param>
	/// <param name="e">イベント引数。</param>
	protected async void FileContextMenu_Opening(object sender, object e) {
		if (sender is not MenuFlyout menuFlyout || this.ViewModel is null) {
			return;
		}

		var fvm = this._contextMenuTargetFile;
		if (fvm is null) {
			return;
		}

		var itemsToRemove = menuFlyout.Items.Where(x => x.Tag?.ToString()?.StartsWith("ExecuteProgram:") == true).ToList();
		foreach (var item in itemsToRemove) {
			menuFlyout.Items.Remove(item);
		}
		var sepToRemove = menuFlyout.Items.FirstOrDefault(x => x.Tag?.ToString() == "ExecuteProgramSeparator");
		if (sepToRemove != null) {
			menuFlyout.Items.Remove(sepToRemove);
		}

		var programs = this.ViewModel.GetExternalPrograms(fvm.MediaType);

		if (programs.Count > 0) {
			int index = 0;
			foreach (var program in programs) {
				var name = string.IsNullOrWhiteSpace(program.Name.Value)
					? this._stringProvider.GetString("Viewer_ContextOpenWithExternal")
					: this._stringProvider.GetString("Viewer_ContextOpenWithFormat", program.Name.Value);
				var item = new MenuFlyoutItem {
					Text = program.IsDefault.Value ? $"{name}{this._stringProvider.GetString("Viewer_ContextDefaultSuffix")}" : name,
					Tag = $"ExecuteProgram:{program.GetHashCode()}",
				};
				item.Click += async (s, args) => {
					var targetFiles = this.ViewModel.MediaContentLibraryViewModel.SelectedFiles.Value;
					var filesToExecute = targetFiles is { Length: > 0 } && targetFiles.Contains(fvm) ? targetFiles : [fvm];
					await this.ViewModel.ExecuteFilesAsync(program, filesToExecute);
				};
				menuFlyout.Items.Insert(index++, item);
			}
			var separator = new MenuFlyoutSeparator { Tag = "ExecuteProgramSeparator" };
			menuFlyout.Items.Insert(index, separator);
		}

		var addRoot = menuFlyout.Items.OfType<MenuFlyoutSubItem>().FirstOrDefault(x => x.Tag?.ToString() == AddToAlbumRootTag);
		if (addRoot is null) {
			return;
		}

		addRoot.Items.Clear();
		var paths = await this.ViewModel.GetRecentAlbumPathsAsync(12);
		foreach (var path in paths) {
			var item = new MenuFlyoutItem {
				Text = path,
				Tag = $"{AddToAlbumItemTagPrefix}{path}",
			};
			item.Click += this.MenuFlyoutItem_Click;
			addRoot.Items.Add(item);
		}
		if (addRoot.Items.Count > 0) {
			addRoot.Items.Add(new MenuFlyoutSeparator());
		}
		var createItem = new MenuFlyoutItem {
			Text = this._stringProvider.GetString("Viewer_ContextCreateNewAlbum"),
			Tag = AddToAlbumCreateTag,
		};
		createItem.Click += this.MenuFlyoutItem_Click;
		addRoot.Items.Add(createItem);
	}

	private async Task CreateAlbumAndAddItemsAsync(IMediaItemViewModel[] targetFiles) {
		if (this.ViewModel is null) {
			return;
		}
		var dialog = Ioc.Default.GetRequiredService<NewAlbumDialog>();
		dialog.XamlRoot = this.XamlRoot;
		dialog.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;
		var result = await dialog.ShowAsync();
		if (result != ContentDialogResult.Primary || string.IsNullOrWhiteSpace(dialog.FullAlbumPath)) {
			return;
		}
		await this.ViewModel.AddToAlbumAsync(dialog.FullAlbumPath, targetFiles);
	}

	/// <summary>
	/// Ctrl + マウスホイールによるアイテムサイズ変更ハンドラ。
	/// </summary>
	/// <param name="sender">イベント発生元。</param>
	/// <param name="e">イベント引数。</param>
	protected void HandleListPointerWheelChanged(object sender, PointerRoutedEventArgs e) {
		var ctrlKeyState = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control);
		if (!ctrlKeyState.HasFlag(CoreVirtualKeyStates.Down)) {
			return;
		}
		var delta = e.GetCurrentPoint(sender as UIElement).Properties.MouseWheelDelta;
		this.ViewModel?.AdjustItemSize(delta);
		e.Handled = true;
	}
}

/// <summary>
/// WrapViewer の基底クラス。UserControlBase を介して WrapViewerViewModel を提供します。
/// </summary>
public abstract class WrapViewerUserControl : ViewerPaneBase<WrapViewerViewModel>;

/// <summary>
/// ListViewer の基底クラス。UserControlBase を介して ListViewerViewModel を提供します。
/// </summary>
public abstract class ListViewerUserControl : ViewerPaneBase<ListViewerViewModel>;

/// <summary>
/// DetailViewer の基底クラス。UserControlBase を介して DetailViewerViewModel を提供します。
/// </summary>
public abstract class DetailViewerUserControl : ViewerPaneBase<DetailViewerViewModel>;

/// <summary>
/// MapViewer の基底クラス。UserControlBase を介して MapViewerViewModel を提供します。
/// </summary>
public abstract class MapViewerUserControl : ViewerPaneBase<MapViewerViewModel>;