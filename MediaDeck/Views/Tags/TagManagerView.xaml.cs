using System.Threading.Tasks;
using CommunityToolkit.Mvvm.DependencyInjection;
using MediaDeck.Composition.Interfaces;
using MediaDeck.Core.Stores.Config;
using MediaDeck.ViewModels.Tags;
using MediaDeck.Views.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;

namespace MediaDeck.Views.Tags;

/// <summary>
/// タグおよびタグカテゴリを管理するビュー。
/// </summary>
public sealed partial class TagManagerView {
	private readonly CompositeDisposable _vmSubscriptions = new();
	private bool _isDialogOpen;

	/// <summary>
	/// <see cref="TagManagerView"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	public TagManagerView() {
		this.InitializeComponent();
		this.Loaded += (_, _) => this.SubscribeViewModel(this.ViewModel);
		this.Unloaded += (_, _) => this._vmSubscriptions.Clear();
	}

	/// <inheritdoc />
	protected override void OnViewModelChanged(TagManagerViewModel? oldViewModel, TagManagerViewModel? newViewModel) {
		base.OnViewModelChanged(oldViewModel, newViewModel);

		this._vmSubscriptions.Clear();
		if (this.IsLoaded && newViewModel != null) {
			this.SubscribeViewModel(newViewModel);
		}
	}

	private void SubscribeViewModel(TagManagerViewModel? vm) {
		this._vmSubscriptions.Clear();
		if (vm == null) {
			return;
		}

		vm.AddTagCategoryRequested.SubscribeAwait(async (_, ct) => {
			await this.ShowAddTagCategoryDialogAsync();
		}, AwaitOperation.Drop).AddTo(this._vmSubscriptions);

		vm.EditTagCategoryRequested.SubscribeAwait(async (category, ct) => {
			await this.ShowEditTagCategoryDialogAsync(category);
		}, AwaitOperation.Drop).AddTo(this._vmSubscriptions);
	}

	private async Task ShowAddTagCategoryDialogAsync() {
		var xamlRoot = this.XamlRoot ?? this.Content?.XamlRoot;
		if (xamlRoot == null || this._isDialogOpen) {
			return;
		}

		this._isDialogOpen = true;
		try {
			var dialog = Ioc.Default.GetRequiredService<TagCategoryEditDialog>();
			dialog.XamlRoot = xamlRoot;
			dialog.ViewModel.InitializeForCreate();

			if (await dialog.ShowAsync() == ContentDialogResult.Primary && dialog.ViewModel.ResultCategory != null) {
				this.ViewModel?.SelectTagCategory(dialog.ViewModel.ResultCategory);
			}
		} finally {
			this._isDialogOpen = false;
		}
	}

	private async Task ShowEditTagCategoryDialogAsync(TagCategoryViewModel target) {
		var xamlRoot = this.XamlRoot ?? this.Content?.XamlRoot;
		if (xamlRoot == null || !target.IsEditable || this._isDialogOpen) {
			return;
		}

		this._isDialogOpen = true;
		try {
			var dialog = Ioc.Default.GetRequiredService<TagCategoryEditDialog>();
			dialog.XamlRoot = xamlRoot;
			dialog.ViewModel.InitializeForEdit(target.Model);

			await dialog.ShowAsync();
		} finally {
			this._isDialogOpen = false;
		}
	}

	private async Task ConfirmAndDeleteTagCategoryAsync(TagCategoryViewModel? target) {
		var xamlRoot = this.XamlRoot ?? this.Content?.XamlRoot;
		if (xamlRoot == null || target is not { IsDeletable: true } || this.ViewModel == null || this._isDialogOpen) {
			return;
		}

		this._isDialogOpen = true;
		try {
			var stringProvider = Ioc.Default.GetRequiredService<IStringProvider>();
			var dialog = new ContentDialog {
				XamlRoot = xamlRoot,
				Title = string.Format(stringProvider.GetString("TagManager_DeleteCategoryConfirmation_Title"), target.TagCategoryName.Value),
				Content = stringProvider.GetString("TagManager_DeleteCategoryConfirmation_Content"),
				PrimaryButtonText = stringProvider.GetString("DialogButton_Delete"),
				CloseButtonText = stringProvider.GetString("DialogButton_Cancel"),
				DefaultButton = ContentDialogButton.Close
			};
			using var disposable = new CompositeDisposable();
			ThemeHelper.BindTheme(dialog, Ioc.Default.GetRequiredService<IConfigStore>(), disposable);
			if (await dialog.ShowAsync() == ContentDialogResult.Primary) {
				this.ViewModel.DeleteTagCategoryCommand.Execute(target);
			}
		} finally {
			this._isDialogOpen = false;
		}
	}

	private void TagCategoryItem_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e) {
		if (sender is FrameworkElement { Tag: TagCategoryViewModel category } && category.IsEditable) {
			this.ViewModel?.EditTagCategoryCommand.Execute(category);
		}
	}

	private void TagCategoryItem_RightTapped(object sender, RightTappedRoutedEventArgs e) {
		if (sender is FrameworkElement element) {
			FlyoutBase.ShowAttachedFlyout(element);
		}
	}

	private void EditTagCategoryMenuItem_Click(object sender, RoutedEventArgs e) {
		if (sender is FrameworkElement { Tag: TagCategoryViewModel category }) {
			this.ViewModel?.EditTagCategoryCommand.Execute(category);
		}
	}

	private async void DeleteTagCategoryMenuItem_Click(object sender, RoutedEventArgs e) {
		if (sender is FrameworkElement { Tag: TagCategoryViewModel category }) {
			await this.ConfirmAndDeleteTagCategoryAsync(category);
		}
	}

	private async void DeleteTagCategoryButton_Click(object sender, RoutedEventArgs e) {
		await this.ConfirmAndDeleteTagCategoryAsync(this.ViewModel?.SelectedTagCategory.Value);
	}

	private async void TagDetailView_DeleteTagRequested(object? sender, EventArgs e) {
		var xamlRoot = this.XamlRoot ?? this.Content?.XamlRoot;
		if (xamlRoot == null || this.ViewModel?.SelectedTagCategory.Value?.SelectedTag.Value == null || this._isDialogOpen) {
			return;
		}

		this._isDialogOpen = true;
		try {
			var stringProvider = Ioc.Default.GetRequiredService<IStringProvider>();
			var dialog = new ContentDialog {
				XamlRoot = xamlRoot,
				Title = stringProvider.GetString("TagManager_DeleteTagConfirmation_Title"),
				Content = stringProvider.GetString("TagManager_DeleteTagConfirmation_Content"),
				PrimaryButtonText = stringProvider.GetString("DialogButton_Delete"),
				CloseButtonText = stringProvider.GetString("DialogButton_Cancel"),
				DefaultButton = ContentDialogButton.Close
			};
			using var disposable = new CompositeDisposable();
			ThemeHelper.BindTheme(dialog, Ioc.Default.GetRequiredService<IConfigStore>(), disposable);
			if (await dialog.ShowAsync() == ContentDialogResult.Primary) {
				this.ViewModel.DeleteTagCommand.Execute(R3.Unit.Default);
			}
		} finally {
			this._isDialogOpen = false;
		}
	}
}

/// <summary>
/// <see cref="TagManagerView"/> の基底ユーザーコントロール。
/// </summary>
public class TagManagerViewUserControl : UserControlBase<TagManagerViewModel>;