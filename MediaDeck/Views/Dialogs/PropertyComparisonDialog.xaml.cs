using System.Collections.Generic;

using MediaDeck.Composition.Enum;
using MediaDeck.Core.Models.Files.SearchConditions;
using MediaDeck.Core.Primitives;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace MediaDeck.Views.Dialogs;

/// <summary>
/// <c>prop.&lt;Name&gt;</c> 検索候補が選択された際に表示される、比較演算子と値を入力するダイアログ。
/// </summary>
public sealed partial class PropertyComparisonDialog : ContentDialog {
	private static readonly Dictionary<SearchTypeComparison, string> OperatorLabels = new() {
		{ SearchTypeComparison.GreaterThan, "を超える (>)" },
		{ SearchTypeComparison.GreaterThanOrEqual, "以上 (>=)" },
		{ SearchTypeComparison.Equal, "と等しい (=)" },
		{ SearchTypeComparison.LessThanOrEqual, "以下 (<=)" },
		{ SearchTypeComparison.LessThan, "未満 (<)" },
	};

	private readonly MediaItemPropertyDescriptor _descriptor;

	public PropertyComparisonDialog(MediaItemPropertyDescriptor descriptor) {
		this.InitializeComponent();
		this._descriptor = descriptor;
		this.PropertyNameText.Text = $"prop.{descriptor.Name}  ({descriptor.ValueType.Name})";

		var items = descriptor.SupportedOperators
			.Select(op => new DisplayObject<SearchTypeComparison>(OperatorLabels[op], op))
			.ToList();
		this.OperatorComboBox.ItemsSource = items;
		this.OperatorComboBox.SelectedIndex = 0;

		this.PrimaryButtonClick += this.OnPrimaryButtonClick;
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
		if (this.OperatorComboBox.SelectedItem is not DisplayObject<SearchTypeComparison> opItem) {
			args.Cancel = true;
			this.ShowError("演算子を選択してください。");
			return;
		}
		var raw = this.ValueTextBox.Text ?? string.Empty;
		if (string.IsNullOrWhiteSpace(raw)) {
			args.Cancel = true;
			this.ShowError("値を入力してください。");
			return;
		}
		// 入力値が型として解釈可能か Build で検証する
		if (this._descriptor.Build(opItem.Value, raw) is null) {
			args.Cancel = true;
			this.ShowError($"値を {this._descriptor.ValueType.Name} として解釈できません。");
			return;
		}
		this.SelectedOperator = opItem.Value;
		this.SelectedValue = raw;
	}

	private void ShowError(string message) {
		this.ErrorText.Text = message;
		this.ErrorText.Visibility = Visibility.Visible;
	}

	private void ValueTextBox_Loaded(object sender, RoutedEventArgs e) {
		this.ValueTextBox.Focus(FocusState.Programmatic);
	}
}