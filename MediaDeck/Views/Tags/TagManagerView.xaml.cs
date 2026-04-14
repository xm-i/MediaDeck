using MediaDeck.ViewModels.Tags;
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

		var dialog = new ContentDialog {
			XamlRoot = this.Content.XamlRoot,
			Title = "このカテゴリを削除しますか？",
			Content = "カテゴリに含まれるタグは「未設定」に移動します。この操作は元に戻せません。",
			PrimaryButtonText = "削除",
			CloseButtonText = "キャンセル",
			DefaultButton = ContentDialogButton.Close
		};
		if (await dialog.ShowAsync() == ContentDialogResult.Primary) {
			this.ViewModel.DeleteTagCategoryCommand.Execute(R3.Unit.Default);
		}
	}

	private async void DeleteTagButton_Click(object sender, RoutedEventArgs e) {
		if (this.ViewModel?.SelectedTagCategory.Value?.SelectedTag.Value == null)
			return;
		var dialog = new ContentDialog {
			XamlRoot = this.Content.XamlRoot,
			Title = "このタグを削除しますか？",
			Content = "この操作は元に戻せません。関連するファイルからのタグ付けも解除されます。",
			PrimaryButtonText = "削除",
			CloseButtonText = "キャンセル",
			DefaultButton = ContentDialogButton.Close
		};
		if (await dialog.ShowAsync() == ContentDialogResult.Primary) {
			this.ViewModel.DeleteTagCommand.Execute(R3.Unit.Default);
		}
	}
}

public class TagManagerViewUserControl : UserControlBase<TagManagerViewModel>;