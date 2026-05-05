using System.Threading.Tasks;
using CommunityToolkit.Mvvm.DependencyInjection;
using MediaDeck.Core.Primitives;
using MediaDeck.Services;
using MediaDeck.ViewModels.Panes.DetailPanes;
using MediaDeck.ViewModels.Tags;
using MediaDeck.Views.Tags;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Windows.System;

namespace MediaDeck.Views.Panes.DetailPanes;

public sealed partial class TagsDetail {
	private IDisposable? _newTagRequestedSubscription;
	private IDisposable? _openTagManagerRequestedSubscription;

	public TagsDetail() {
		this.InitializeComponent();
	}


	protected override void OnViewModelChanged(DetailSelectorViewModel? oldViewModel, DetailSelectorViewModel? newViewModel) {
		base.OnViewModelChanged(oldViewModel, newViewModel);

		this._newTagRequestedSubscription?.Dispose();
		this._newTagRequestedSubscription = newViewModel?.NewTagRequested.Subscribe(async context => {
			await this.ShowNewTagDialogAsync(context);
		});

		this._openTagManagerRequestedSubscription?.Dispose();
		this._openTagManagerRequestedSubscription = newViewModel?.OpenTagManagerRequested.Subscribe(tag => {
			var window = Ioc.Default.GetRequiredService<TagManagerWindow>();
			window.ViewModel.SelectTag(tag.Model.TagId);

			var windowManager = Ioc.Default.GetRequiredService<WindowManager>();
			var windowService = Ioc.Default.GetRequiredService<WindowService>();
			var parent = windowManager.GetWindowFromElement(this);
			if (parent == null) {
				// TODO: notify;
				return;
			}
			windowService.ActivateCenteredOnMainWindow(window, parent);
		});
	}

	private async Task ShowNewTagDialogAsync(NewTagRequestedContext context) {
		if (this.XamlRoot == null || this.ViewModel == null) {
			return;
		}

		var dialog = Ioc.Default.GetRequiredService<NewTagDialog>();
		dialog.XamlRoot = this.XamlRoot;
		dialog.ViewModel.TagName.Value = context.TagName;
		dialog.ViewModel.SelectedCategory.Value = dialog.ViewModel.TagCategories.FirstOrDefault(x => x.Model == context.SelectedCategory);

		var result = await dialog.ShowAsync();
		if (result == ContentDialogResult.Primary && dialog.ViewModel.CreatedTag.Value is { }) {
			// タグ作成済み。ViewModel に紐付けのみ依頼
			await this.ViewModel.OnNewTagCreated(dialog.ViewModel.CreatedTag.Value);
		}
	}

	private void AutoSuggestBox_PreviewKeyDown(object sender, KeyRoutedEventArgs e) {
		if (e.Key == VirtualKey.Enter) {
			this.ViewModel?.AddTagCommand.Execute(Unit.Default);
		}
	}

	private void AutoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args) {
		if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput) {
			this.ViewModel?.RefreshFilteredTagCandidatesCommand.Execute(Unit.Default);
		}
	}

	private void AutoSuggestBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args) {
		if (args.ChosenSuggestion is TagViewModel tagViewModel) {
			this.ViewModel?.AddSpecificTagCommand.Execute(tagViewModel);
		}
	}


	private void TagGrid_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e) {
		if (this.TagListBox.SelectedItem is not ValueCountPair<TagViewModel> tag) {
			return;
		}
		this.ViewModel?.SearchTaggedFilesCommand.Execute(tag);
	}

	private void TagGrid_RightTapped(object sender, RightTappedRoutedEventArgs e) {
		if (sender is Grid grid) {
			FlyoutBase.ShowAttachedFlyout(grid);
		}
	}
}