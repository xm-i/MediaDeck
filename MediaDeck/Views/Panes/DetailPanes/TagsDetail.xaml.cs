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

/// <summary>
/// タグ詳細表示および編集を行うビュー。
/// </summary>
public sealed partial class TagsDetail {
	private readonly CompositeDisposable _vmSubscriptions = new();

	/// <summary>
	/// <see cref="TagsDetail"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	public TagsDetail() {
		this.InitializeComponent();
		this.Loaded += (_, _) => this.SubscribeViewModel(this.ViewModel);
		this.Unloaded += (_, _) => this._vmSubscriptions.Clear();
	}

	/// <inheritdoc />
	protected override void OnViewModelChanged(TagsDetailViewModel? oldViewModel, TagsDetailViewModel? newViewModel) {
		base.OnViewModelChanged(oldViewModel, newViewModel);

		this._vmSubscriptions.Clear();
		if (this.IsLoaded && newViewModel != null) {
			this.SubscribeViewModel(newViewModel);
		}
	}

	private void SubscribeViewModel(TagsDetailViewModel? vm) {
		this._vmSubscriptions.Clear();
		if (vm is null) {
			return;
		}

		vm.NewTagRequested.SubscribeAwait(async (context, ct) => {
			await this.ShowNewTagDialogAsync(context);
		}, AwaitOperation.Drop).AddTo(this._vmSubscriptions);

		vm.OpenTagManagerRequested.Subscribe(tag => {
			var windowService = Ioc.Default.GetRequiredService<IWindowService>();
			windowService.ActivateCentered<TagManagerWindow>(this, window => {
				window.ViewModel.SelectTag(tag.Model.TagId);
			});
		}).AddTo(this._vmSubscriptions);
	}

	private async Task ShowNewTagDialogAsync(NewTagRequestedContext context) {
		if (this.XamlRoot == null || this.ViewModel == null) {
			return;
		}

		var dialog = Ioc.Default.GetRequiredService<NewTagDialog>();
		dialog.XamlRoot = this.XamlRoot;
		dialog.ViewModel.TagName.Value = context.TagName;
		var selectedCategory = dialog.ViewModel.TagCategories.FirstOrDefault(x => x.Model == context.SelectedCategory);
		dialog.ViewModel.SelectedCategory.Value = selectedCategory;

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

/// <summary>
/// TagsDetail の基底クラス。UserControlBase を介して ViewModel プロパティを提供します。
/// </summary>
public abstract class TagsDetailUserControl : UserControlBase<TagsDetailViewModel>;