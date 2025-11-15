using System.Threading.Tasks;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace MediaDeck.Utils.Tools; 
public class DialogUtility {
	public static async Task ConfirmDialogAndAction<TResult>(
		XamlRoot xamlRoot,
		Func<TResult> action,
		string confirmTitle,
		Func<TResult, string> resultTitle
		) {
		var dialog = new ContentDialog {
			XamlRoot = xamlRoot,
			Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
			Title = confirmTitle,
			PrimaryButtonText = "Yes",
			SecondaryButtonText = "No",
			CloseButtonText = "Cancel",
			DefaultButton = ContentDialogButton.Primary
		};

		var result = await dialog.ShowAsync();
		if (result == ContentDialogResult.Primary) {
			var actionResult = action();

			var dialog2 = new ContentDialog {
				XamlRoot = xamlRoot,
				Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
				Title = resultTitle(actionResult),
				PrimaryButtonText = "OK",
				DefaultButton = ContentDialogButton.Primary
			};

			await dialog2.ShowAsync();
		}
	}
}
