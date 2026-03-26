using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.WinUI.Controls;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

using MediaDeck.Composition.Bases;
using MediaDeck.Models.Files.SearchConditions;
using MediaDeck.ViewModels.Panes.ViewerPanes;
using MediaDeck.Views.Thumbnails;

using System.Diagnostics;
using System.IO;
using Microsoft.UI.Input;
using Windows.UI.Core;
using MediaDeck.Composition.Interfaces.FileTypes.ViewModels;

namespace MediaDeck.Views.Panes.ViewerPanes;

public class ViewerPaneBase : UserControlBase<ViewerSelectorViewModel> {
	protected virtual void List_SelectionChanged(object sender, SelectionChangedEventArgs e) {
		if (this.ViewModel is not { } vm) {
			return;
		}
		if (sender is ListBox listBox) {
			vm.MediaContentLibraryViewModel.SelectedFiles.Value = listBox.SelectedItems.Select(x => x as IFileViewModel).Where(x => x is not null).ToArray()!;
		} else if (sender is GridView gridView) {
			vm.MediaContentLibraryViewModel.SelectedFiles.Value = gridView.SelectedItems.Select(x => x as IFileViewModel).Where(x => x is not null).ToArray()!;
		}
	}

	protected async void File_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e) {
		if (sender is not Grid grid) {
			return;
		}
		if (grid.DataContext is not IFileViewModel fileViewModel) {
			return;
		}
		await fileViewModel.ExecuteFileAsync();
	}

	protected void List_RightTapped(object sender, RightTappedRoutedEventArgs e) {
		if (sender is not FrameworkElement parentControl) {
			return;
		}

		var element = e.OriginalSource as FrameworkElement;
		while (element != null && element.DataContext is not IFileViewModel) {
			element = element.Parent as FrameworkElement;
		}

		if (element?.DataContext is not IFileViewModel fileViewModel) {
			return;
		}

		if (parentControl.Resources["FileContextMenu"] is not MenuFlyout menuFlyout) {
			return;
		}

		menuFlyout.ShowAt(element, e.GetPosition(element));
	}


	protected async void MenuFlyoutItem_Click(object sender, RoutedEventArgs e) {
		if (this.ViewModel is null) {
			return;
		}
		if (sender is not MenuFlyoutItem selectedItem) {
			return;
		}
		if (selectedItem.DataContext is not IFileViewModel fvm) {
			return;
		}
		switch (selectedItem.Tag.ToString()) {
			case "RecreateThumbnail":
				var window = Ioc.Default.GetRequiredService<ThumbnailPickerWindow>();
				window.ViewModel.FileViewModel.Value = fvm;
				window.ActivateCenteredOnMainWindow();
				break;
			case "RemoveFile":
				var selectedFiles = this.ViewModel.MediaContentLibraryViewModel.SelectedFiles.Value;
				var targetFiles = selectedFiles is { Length: > 0 } && selectedFiles.Contains(fvm)
					? selectedFiles
					: [fvm];
				var message = targetFiles.Length == 1
					? "Remove file from MediaDeck database?"
					: $"Remove {targetFiles.Length} files from MediaDeck database?";
				
				var dialog = new ContentDialog {
					XamlRoot = this.XamlRoot,
					Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
					Title = message,
					PrimaryButtonText = "Yes",
					SecondaryButtonText = "No",
					CloseButtonText = "Cancel",
					DefaultButton = ContentDialogButton.Primary
				};

				var result = await dialog.ShowAsync();
				if (result == ContentDialogResult.Primary) {
					await this.ViewModel.SelectedViewerPane.Value.RemoveFilesAsync(targetFiles);
					await this.ViewModel.MediaContentLibraryViewModel.ReloadAsync();
				}
				break;
			case "OpenFolder":
				if (!string.IsNullOrEmpty(fvm.FilePath) && File.Exists(fvm.FilePath)) {
					Process.Start(new ProcessStartInfo {
						FileName = "explorer.exe",
						Arguments = $"/select, \"{fvm.FilePath}\"",
						UseShellExecute = true
					});
				}
				break;
		}
	}

	protected void TokenizingTextBox_TokenItemAdding(TokenizingTextBox sender, TokenItemAddingEventArgs args) {
		args.Cancel = true;
		this.ViewModel?.MediaContentLibraryViewModel.SearchConditionNotificationDispatcher.AddRequest.OnNext(new WordSearchCondition(args.TokenText));
	}

	protected void TokenizingTextBox_TokenItemRemoving(TokenizingTextBox sender, TokenItemRemovingEventArgs args) {
		args.Cancel = true;
		if (args.Item is not SearchConditionViewModel { } condition) {
			return;
		}
		this.ViewModel?.MediaContentLibraryViewModel.SearchConditionNotificationDispatcher.RemoveRequest.OnNext(condition.SearchCondition);
	}
	protected void TokenizingTextBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args) {
		if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput) {
			this.ViewModel?.MediaContentLibraryViewModel.RefreshSearchTokenCandidates(sender.Text);
		}
	}

	protected void HandleListPointerWheelChanged(object sender, PointerRoutedEventArgs e) {
		var ctrlKeyState = InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Control);
		if (ctrlKeyState.HasFlag(CoreVirtualKeyStates.Down)) {
			var delta = e.GetCurrentPoint(sender as UIElement).Properties.MouseWheelDelta;
			if (this.ViewModel != null) {
				var step = 20;
				if (delta > 0) {
					this.ViewModel.ItemSize.Value = Math.Min(500, this.ViewModel.ItemSize.Value + step);
				} else if (delta < 0) {
					this.ViewModel.ItemSize.Value = Math.Max(20, this.ViewModel.ItemSize.Value - step);
				}
			}
			e.Handled = true;
		}
	}
}