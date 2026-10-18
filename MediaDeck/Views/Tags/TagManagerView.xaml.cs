using CommunityToolkit.Mvvm.DependencyInjection;
using MediaDeck.Composition.Interfaces;
using MediaDeck.Core.Stores.Config;
using MediaDeck.ViewModels.Tags;
using MediaDeck.Views.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace MediaDeck.Views.Tags;

public sealed partial class TagManagerView {
	public TagManagerView() {
		this.InitializeComponent();
	}

	private async void DeleteTagCategoryButton_Click(object sender, RoutedEventArgs e) {
		if (this.ViewModel?.SelectedTagCategory.Value == null)
			return;

		var stringProvider = Ioc.Default.GetRequiredService<IStringProvider>();
		var dialog = new ContentDialog {
			XamlRoot = this.Content.XamlRoot,
			Title = stringProvider.GetString("TagManager_DeleteCategoryConfirmation_Title"),
			Content = stringProvider.GetString("TagManager_DeleteCategoryConfirmation_Content"),
			PrimaryButtonText = stringProvider.GetString("DialogButton_Delete"),
			CloseButtonText = stringProvider.GetString("DialogButton_Cancel"),
			DefaultButton = ContentDialogButton.Close
		};
		using var disposable = new CompositeDisposable();
		ThemeHelper.BindTheme(dialog, Ioc.Default.GetRequiredService<IConfigStore>(), disposable);
		if (await dialog.ShowAsync() == ContentDialogResult.Primary) {
			this.ViewModel.DeleteTagCategoryCommand.Execute(R3.Unit.Default);
		}
	}

	private async void TagDetailView_DeleteTagRequested(object? sender, EventArgs e) {
		if (this.ViewModel?.SelectedTagCategory.Value?.SelectedTag.Value == null)
			return;
		var stringProvider = Ioc.Default.GetRequiredService<IStringProvider>();
		var dialog = new ContentDialog {
			XamlRoot = this.Content.XamlRoot,
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
	}
}

public class TagManagerViewUserControl : UserControlBase<TagManagerViewModel>;