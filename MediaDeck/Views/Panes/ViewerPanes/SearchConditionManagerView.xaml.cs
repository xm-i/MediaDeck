using CommunityToolkit.WinUI.Controls;
using MediaDeck.Core.Models.Files.SearchConditions;
using MediaDeck.ViewModels.Panes.ViewerPanes;
using Microsoft.UI.Xaml.Controls;

namespace MediaDeck.Views.Panes.ViewerPanes;

public sealed partial class SearchConditionManagerView {
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

	private void TokenizingTextBox_TokenItemAdded(TokenizingTextBox sender, object args) {
		this.ViewModel?.SearchConditionNotificationDispatcher.SearchConditionChanged.OnNext(Unit.Default);
	}
}

public abstract class SearchConditionManagerViewUserControl : UserControlBase<SearchConditionManagerViewModel>;