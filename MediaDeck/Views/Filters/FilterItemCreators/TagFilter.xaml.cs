using System.Windows.Input;

using MediaDeck.ViewModels.Filters.FilterItemCreators;

using Microsoft.UI.Xaml.Controls;

namespace MediaDeck.Views.Filters.FilterItemCreators;

/// <summary>
/// タグフィルター入力用コントロール。
/// </summary>
public partial class TagFilter {
	/// <summary>
	/// <see cref="TagFilter"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	public TagFilter() {
		this.InitializeComponent();
	}

	private void AutoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args) {
		if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput) {
			this.ViewModel?.RefreshTagSuggestions(sender.Text);
		}
	}

	private void AutoSuggestBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args) {
		if (args.SelectedItem is TagSuggestionItem suggestion && this.ViewModel is not null) {
			this.ViewModel.TagName.Value = suggestion.TagName;
		}
	}

	private void AutoSuggestBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args) {
		if (this.ViewModel is null) {
			return;
		}
		if (args.ChosenSuggestion is TagSuggestionItem suggestion) {
			this.ViewModel.TagName.Value = suggestion.TagName;
		}
		if (this.ViewModel.AddFilterCommand is ICommand command && command.CanExecute(null)) {
			command.Execute(null);
		}
	}
}

/// <summary>
/// <see cref="TagFilter"/> の基底クラス。
/// </summary>
public class TagFilterControlBase : UserControlBase<TagFilterCreatorViewModel>;