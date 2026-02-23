using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;

using MediaDeck.Utils.Objects;
using MediaDeck.Composition.Interfaces.Files;
using MediaDeck.Views.Tags;
using MediaDeck.ViewModels.Tags;
using MediaDeck.ViewModels.Panes.DetailPanes;

using System.Threading.Tasks;

namespace MediaDeck.Views.Panes.DetailPanes;

public sealed partial class TagsDetail : DetailPaneBase {
	private IDisposable? _newTagRequestedSubscription;

	public TagsDetail() {
		this.InitializeComponent();
	}
	

	protected override void OnViewModelChanged(DetailSelectorViewModel? oldViewModel, DetailSelectorViewModel? newViewModel) {
		base.OnViewModelChanged(oldViewModel, newViewModel);

		this._newTagRequestedSubscription?.Dispose();
		this._newTagRequestedSubscription = newViewModel?.NewTagRequested.Subscribe(async context => {
			await this.ShowNewTagDialogAsync(context);
		});
	}

	private async Task ShowNewTagDialogAsync(NewTagRequestedContext context) {
		if (this.XamlRoot == null || this.ViewModel == null) {
			return;
		}

		var tagsManager = this.ViewModel.GetTagsManager();
		var newTagDialogViewModel = new NewTagDialogViewModel(tagsManager);
		var dialog = new NewTagDialog(newTagDialogViewModel) {
			XamlRoot = this.XamlRoot
		};
		dialog.ViewModel.TagName.Value = context.TagName;
		dialog.ViewModel.SelectedCategory.Value = context.TagCategories.FirstOrDefault();

		var result = await dialog.ShowAsync();
		if (result == ContentDialogResult.Primary && dialog.ViewModel.CreatedTag.Value is { }) {
			// タグ作成済み。ViewModel に紐付けのみ依頼
			await this.ViewModel.OnNewTagCreated(dialog.ViewModel.CreatedTag.Value);
		}
	}

	private void AutoSuggestBox_PreviewKeyDown(object sender, KeyRoutedEventArgs e) {
		if (e.Key == Windows.System.VirtualKey.Enter) {
			this.ViewModel?.AddTagCommand.Execute(Unit.Default);
		}
	}

	private void AutoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args) {
		if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput) {
			this.ViewModel?.RefreshFilteredTagCandidatesCommand.Execute(Unit.Default);
		}
	}

	private void TagGrid_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e) {
		if (this.TagListBox.SelectedItem is not ValueCountPair<ITagModel> tag) {
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