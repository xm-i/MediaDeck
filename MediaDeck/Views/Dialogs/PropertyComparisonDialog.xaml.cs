using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces;
using MediaDeck.Core.Models.Files.SearchConditions;
using MediaDeck.Core.Stores.Config;
using MediaDeck.ViewModels.Dialogs;
using MediaDeck.Views.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace MediaDeck.Views.Dialogs;

/// <summary>
/// <c>prop.&lt;Name&gt;</c> 検索候補が選択された際に表示される、比較演算子と値を入力するダイアログ。
/// </summary>
[Inject(InjectServiceLifetime.Transient)]
public sealed partial class PropertyComparisonDialog : ContentDialog {

	public PropertyComparisonDialogViewModel ViewModel {
		get;
	}

	private readonly IStringProvider _stringProvider;
	private readonly CompositeDisposable _disposable = new();

	public PropertyComparisonDialog(PropertyComparisonDialogViewModel viewModel, IConfigStore configStore, IStringProvider stringProvider) {
		this.ViewModel = viewModel;
		this._stringProvider = stringProvider;
		this.InitializeComponent();

		ThemeHelper.BindTheme(this, configStore, this._disposable);

		this.PrimaryButtonClick += this.OnPrimaryButtonClick;
		this.Closed += (_, _) => {
			this.ViewModel.Dispose();
			this._disposable.Dispose();
		};
	}

	public void Initialize(MediaItemPropertyDescriptor descriptor) {
		this.ViewModel.Initialize(descriptor);
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
			this.ShowError(this._stringProvider.GetString("PropertyComparisonDialog_Error_SelectOperator"));
			return;
		}
		var raw = this.ViewModel.GetRawValueString();
		if (string.IsNullOrWhiteSpace(raw)) {
			args.Cancel = true;
			this.ShowError(this._stringProvider.GetString("PropertyComparisonDialog_Error_EnterValue"));
			return;
		}
		// 入力値が型として解釈可能か Build で検証する
		if (this.ViewModel.Descriptor.Build(this.ViewModel.SelectedOperator.Value.Value, raw) is null) {
			args.Cancel = true;
			this.ShowError(this._stringProvider.GetString("PropertyComparisonDialog_Error_CannotParseValue", this.ViewModel.Descriptor.ValueType.Name));
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