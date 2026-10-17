using MediaDeck.Core.Stores.Config;
using MediaDeck.ViewModels.Tags;
using MediaDeck.Views.Helpers;

namespace MediaDeck.Views.Tags;

[Inject(InjectServiceLifetime.Transient)]
public sealed partial class NewTagDialog {
	private readonly CompositeDisposable _disposable = new();

	/// <summary>
	/// <see cref="NewTagDialog"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	/// <param name="viewModel">新規タグ作成ダイアログViewModel</param>
	/// <param name="configStore">設定ストア</param>
	public NewTagDialog(NewTagDialogViewModel viewModel, IConfigStore configStore) {
		this.ViewModel = viewModel.AddTo(this._disposable);
		this.InitializeComponent();

		ThemeHelper.BindTheme(this, configStore, this._disposable);
		this.Closed += (_, _) => this._disposable.Dispose();

		this.PrimaryButtonClick += (_, _) => {
			this.ViewModel.ConfirmCommand.Execute(Unit.Default);
		};

		this.CloseButtonClick += (_, _) => {
			this.ViewModel.CancelCommand.Execute(Unit.Default);
		};
	}

	/// <summary>
	/// 新規タグ作成ダイアログViewModel
	/// </summary>
	public NewTagDialogViewModel ViewModel {
		get;
	}
}