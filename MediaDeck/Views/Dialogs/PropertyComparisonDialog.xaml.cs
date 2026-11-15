using MediaDeck.Composition.Enum;
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

	/// <summary>ダイアログの ViewModel を取得します。</summary>
	public PropertyComparisonDialogViewModel ViewModel {
		get;
	}

	private readonly CompositeDisposable _disposable = new();

	/// <summary>
	/// <see cref="PropertyComparisonDialog"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	/// <param name="viewModel">ダイアログ ViewModel。</param>
	/// <param name="configStore">設定ストア。</param>
	public PropertyComparisonDialog(PropertyComparisonDialogViewModel viewModel, IConfigStore configStore) {
		this.ViewModel = viewModel;
		this.InitializeComponent();

		ThemeHelper.BindTheme(this, configStore, this._disposable);

		this.PrimaryButtonClick += this.OnPrimaryButtonClick;
		this.Closed += (_, _) => {
			this.ViewModel.Dispose();
			this._disposable.Dispose();
		};
	}

	/// <summary>
	/// 対象プロパティの記述情報を受け取ってダイアログを初期化します。
	/// </summary>
	/// <param name="descriptor">プロパティ記述情報。</param>
	public void Initialize(MediaItemPropertyDescriptor descriptor) {
		this.ViewModel.Initialize(descriptor);
	}

	/// <summary>確定後の比較演算子。</summary>
	public SearchTypeComparison SelectedOperator {
		get {
			return this.ViewModel.ConfirmedOperator;
		}
	}

	/// <summary>確定後の入力値。</summary>
	public string SelectedValue {
		get {
			return this.ViewModel.ConfirmedValue;
		}
	}

	private void OnPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args) {
		if (!this.ViewModel.TryConfirm(out var errorMessage)) {
			args.Cancel = true;
			this.ShowError(errorMessage ?? string.Empty);
		}
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