using MediaDeck.Core.Stores.Config;
using MediaDeck.ViewModels.Tags;
using MediaDeck.Views.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace MediaDeck.Views.Tags;

/// <summary>
/// タグカテゴリの追加・編集を行うダイアログ。
/// </summary>
[Inject(InjectServiceLifetime.Transient)]
public sealed partial class TagCategoryEditDialog : ContentDialog {
	private readonly CompositeDisposable _disposable = new();

	/// <summary>
	/// <see cref="TagCategoryEditDialog"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	/// <param name="viewModel">タグカテゴリ追加・編集ダイアログ ViewModel</param>
	/// <param name="configStore">設定ストア</param>
	public TagCategoryEditDialog(TagCategoryEditDialogViewModel viewModel, IConfigStore configStore) {
		this.ViewModel = viewModel.AddTo(this._disposable);
		this.InitializeComponent();

		ThemeHelper.BindTheme(this, configStore, this._disposable);
		this.Closed += (_, _) => this._disposable.Dispose();
	}

	/// <summary>
	/// タグカテゴリ追加・編集ダイアログ ViewModel を取得します。
	/// </summary>
	public TagCategoryEditDialogViewModel ViewModel {
		get;
	}

	private async void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args) {
		var deferral = args.GetDeferral();
		try {
			if (!await this.ViewModel.SaveAsync()) {
				args.Cancel = true;
			}
		} finally {
			deferral.Complete();
		}
	}

	private void CategoryNameTextBox_Loaded(object sender, RoutedEventArgs e) {
		this.CategoryNameTextBox.Focus(FocusState.Programmatic);
		if (this.ViewModel.IsEditMode) {
			this.CategoryNameTextBox.SelectAll();
		}
	}
}