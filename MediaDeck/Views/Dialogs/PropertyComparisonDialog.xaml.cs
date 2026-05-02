using MediaDeck.Composition.Enum;
using MediaDeck.Core.Models.Files.SearchConditions;
using MediaDeck.ViewModels.Dialogs;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace MediaDeck.Views.Dialogs;

/// <summary>
/// <c>prop.&lt;Name&gt;</c> 検索候補が選択された際に表示される、比較演算子と値を入力するダイアログ。
/// </summary>
public sealed partial class PropertyComparisonDialog : ContentDialog {

	public PropertyComparisonDialogViewModel ViewModel {
		get;
	}

	public PropertyComparisonDialog(MediaItemPropertyDescriptor descriptor) {
		this.ViewModel = new PropertyComparisonDialogViewModel(descriptor);
		this.InitializeComponent();

		this.PrimaryButtonClick += this.OnPrimaryButtonClick;
		this.Closed += (_, _) => this.ViewModel.Dispose();
	}

	/// <summary>確定後の比較演算子。</summary>
	public SearchTypeComparison SelectedOperator {
		get;
		private set;
	}

	/// <summary>確定後の入力値。</summary>
	public string SelectedValue {
		get;
		private set;
	} = string.Empty;

	private void OnPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args) {
		if (this.ViewModel.SelectedOperator.Value == null) {
			args.Cancel = true;
			this.ShowError("演算子を選択してください。");
			return;
		}
		var raw = this.ViewModel.GetRawValueString();
		if (string.IsNullOrWhiteSpace(raw)) {
			args.Cancel = true;
			this.ShowError("値を入力してください。");
			return;
		}
		// 入力値が型として解釈可能か Build で検証する
		if (this.ViewModel.Descriptor.Build(this.ViewModel.SelectedOperator.Value.Value, raw) is null) {
			args.Cancel = true;
			this.ShowError($"値を {this.ViewModel.Descriptor.ValueType.Name} として解釈できません。");
			return;
		}
		this.SelectedOperator = this.ViewModel.SelectedOperator.Value.Value;
		this.SelectedValue = raw;
	}

	private void ShowError(string message) {
		this.ErrorText.Text = message;
		this.ErrorText.Visibility = Visibility.Visible;
	}

	private void ValueControl_Loaded(object sender, RoutedEventArgs e) {
		if (sender is Control control) {
			control.Focus(FocusState.Programmatic);
		}
	}
}