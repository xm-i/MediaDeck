using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.WinUI.Controls;

using MediaDeck.ViewModels.Panes.ViewerPanes;
using MediaDeck.Views.Dialogs;

using Microsoft.UI.Xaml.Controls;

namespace MediaDeck.Views.Panes.ViewerPanes;

/// <summary>
/// 検索条件マネージャの View 実装。
/// </summary>
public sealed partial class SearchConditionManagerView {
	/// <summary>
	/// <see cref="SearchConditionManagerView"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	public SearchConditionManagerView() {
		this.InitializeComponent();
	}

	private void TokenizingTextBox_TokenItemAdding(TokenizingTextBox sender, TokenItemAddingEventArgs args) {
		args.Cancel = true;
		if (!string.IsNullOrWhiteSpace(args.TokenText)) {
			this.ViewModel?.AddWordCondition(args.TokenText);
		}
	}

	private void TokenizingTextBox_TokenItemRemoving(TokenizingTextBox sender, TokenItemRemovingEventArgs args) {
		args.Cancel = true;
		if (args.Item is not SearchConditionViewModel condition) {
			return;
		}
		this.ViewModel?.RemoveCondition(condition);
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
		if (this.ViewModel?.TryGetUnconfiguredPropertyDescriptor(args, out var stub, out var descriptor) == true) {
			var dialog = Ioc.Default.GetRequiredService<PropertyComparisonDialog>();
			dialog.XamlRoot = this.XamlRoot;
			dialog.Initialize(descriptor);
			var result = await dialog.ShowAsync();
			if (result != ContentDialogResult.Primary) {
				this.ViewModel?.RemoveCondition(stub);
				return;
			}
			this.ViewModel?.ConfigurePropertyCondition(stub, descriptor.Name, dialog.SelectedOperator, dialog.SelectedValue);
			return;
		}
		this.ViewModel?.NotifySearchConditionChanged();
	}

	private void TokenizingTextBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args) {
		this.ViewModel?.RefreshCommand.Execute(Unit.Default);
	}
}

/// <summary>
/// 検索条件マネージャ View の基底コントロール。
/// </summary>
public abstract class SearchConditionManagerViewUserControl : UserControlBase<SearchConditionManagerViewModel>;