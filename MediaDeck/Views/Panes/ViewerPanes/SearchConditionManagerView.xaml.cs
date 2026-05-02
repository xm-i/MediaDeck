using System.Threading.Tasks;

using CommunityToolkit.WinUI.Controls;
using MediaDeck.Core.Models.Files.SearchConditions;
using MediaDeck.ViewModels.Panes.ViewerPanes;
using MediaDeck.Views.Dialogs;
using Microsoft.UI.Xaml.Controls;

namespace MediaDeck.Views.Panes.ViewerPanes;

public sealed partial class SearchConditionManagerView {
	private const string PropPrefix = "prop.";

	public SearchConditionManagerView() {
		this.InitializeComponent();
	}

	private void TokenizingTextBox_TokenItemAdding(TokenizingTextBox sender, TokenItemAddingEventArgs args) {
		args.Cancel = true;
		if (!string.IsNullOrWhiteSpace(args.TokenText)) {
			this.ViewModel?.SearchConditionNotificationDispatcher.AddRequest.OnNext(new WordSearchCondition { Word = args.TokenText });
		}
	}

	private void TokenizingTextBox_TokenItemRemoving(TokenizingTextBox sender, TokenItemRemovingEventArgs args) {
		args.Cancel = true;
		if (args.Item is not SearchConditionViewModel condition) {
			return;
		}
		this.ViewModel?.SearchConditionNotificationDispatcher.RemoveRequest.OnNext(condition.SearchCondition);
	}

	private void TokenizingTextBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args) {
		if (args.Reason != AutoSuggestionBoxTextChangeReason.UserInput) {
			return;
		}
		this.ViewModel?.RefreshSearchTokenCandidates(sender.Text);
	}

	private async void TokenizingTextBox_TokenItemAdded(TokenizingTextBox sender, object args) {
		// サジェストから prop. スタブが選択された場合は、未確定のスタブを除去してから
		// 比較ダイアログを表示し、確定された PropertySearchCondition を追加する。
		if (args is SearchConditionViewModel { SearchCondition: PropertySearchCondition { IsConfigured: false } stub }) {
			var descriptor = MediaItemPropertyCatalog.Find(stub.PropertyName);
			if (descriptor != null) {
				var condition = await this.ShowPropertyDialogAndAddAsync(descriptor);
				if (condition == null) {
					this.ViewModel?.SearchConditionNotificationDispatcher.RemoveRequest.OnNext(stub);
					return;
				}
				this.ViewModel?.SearchConditionNotificationDispatcher.UpdateRequest.OnNext(x => {
					if (x.IndexOf(stub) is int i && i >= 0) {
						x.RemoveAt(i);
						x.Insert(i, condition);
					}
				});

			}
			return;
		}
		this.ViewModel?.SearchConditionNotificationDispatcher.SearchConditionChanged.OnNext(Unit.Default);
	}

	private async Task<PropertySearchCondition?> ShowPropertyDialogAndAddAsync(MediaItemPropertyDescriptor descriptor) {
		var dialog = new PropertyComparisonDialog(descriptor) {
			XamlRoot = this.XamlRoot
		};
		var result = await dialog.ShowAsync();
		if (result != ContentDialogResult.Primary) {
			return null;
		}
		var condition = new PropertySearchCondition {
			PropertyName = descriptor.Name,
			Operator = dialog.SelectedOperator,
			Value = dialog.SelectedValue,
			IsConfigured = true,
		};
		return condition;
	}
}

public abstract class SearchConditionManagerViewUserControl : UserControlBase<SearchConditionManagerViewModel>;