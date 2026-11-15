using MediaDeck.Common.Base;
using MediaDeck.Composition.Interfaces.Tags;

namespace MediaDeck.ViewModels.Tags;

/// <summary>
/// 新規タグ作成ダイアログの ViewModel クラス。
/// </summary>
[Inject(InjectServiceLifetime.Transient)]
public class NewTagDialogViewModel : ViewModelBase {
	/// <summary>
	/// <see cref="NewTagDialogViewModel"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	/// <param name="tagsManager">タグマネージャー</param>
	public NewTagDialogViewModel(ITagsManager tagsManager) {
		this.TagName = new BindableReactiveProperty<string>(string.Empty).AddTo(this.CompositeDisposable);
		this.Ruby = new BindableReactiveProperty<string>(string.Empty).AddTo(this.CompositeDisposable);
		this.Detail = new BindableReactiveProperty<string>(string.Empty).AddTo(this.CompositeDisposable);
		this.SelectedCategory = new BindableReactiveProperty<ITagCategoryModel?>().AddTo(this.CompositeDisposable);
		this.Result = new BindableReactiveProperty<DialogResult>(DialogResult.None).AddTo(this.CompositeDisposable);
		this.CreatedTag = new BindableReactiveProperty<ITagModel?>().AddTo(this.CompositeDisposable);
		this.ConfirmCommand = new ReactiveCommand().AddTo(this.CompositeDisposable);
		this.CancelCommand = new ReactiveCommand().AddTo(this.CompositeDisposable);

		this.TagCategories = tagsManager.TagCategories.ToArray();
		var defaultCategory = this.TagCategories.FirstOrDefault();
		this.SelectedCategory.Value = defaultCategory;

		this.ConfirmCommand.SubscribeAwait(async (_, ct) => {
			// タグを作成
			this.CreatedTag.Value = await tagsManager.CreateTagImmediatelyAsync(
				this.SelectedCategory.Value?.TagCategoryId,
				this.TagName.Value,
				this.Ruby.Value,
				this.Detail.Value,
				[]);
			this.Result.Value = DialogResult.Confirmed;
		}, AwaitOperation.Drop).AddTo(this.CompositeDisposable);

		this.CancelCommand.Subscribe(_ => {
			this.Result.Value = DialogResult.Canceled;
		}).AddTo(this.CompositeDisposable);
	}

	/// <summary>
	/// タグ名
	/// </summary>
	public BindableReactiveProperty<string> TagName {
		get;
	}

	/// <summary>
	/// ふりがな
	/// </summary>
	public BindableReactiveProperty<string> Ruby {
		get;
	}

	/// <summary>
	/// 詳細説明
	/// </summary>
	public BindableReactiveProperty<string> Detail {
		get;
	}

	/// <summary>
	/// 選択中のタグカテゴリー
	/// </summary>
	public BindableReactiveProperty<ITagCategoryModel?> SelectedCategory {
		get;
	}

	/// <summary>
	/// ダイアログの結果
	/// </summary>
	public BindableReactiveProperty<DialogResult> Result {
		get;
	}

	/// <summary>
	/// 作成されたタグモデル
	/// </summary>
	public BindableReactiveProperty<ITagModel?> CreatedTag {
		get;
	}

	/// <summary>
	/// タグカテゴリー一覧
	/// </summary>
	public ITagCategoryModel[] TagCategories {
		get;
	}

	/// <summary>
	/// 確定コマンド
	/// </summary>
	public ReactiveCommand ConfirmCommand {
		get;
	}

	/// <summary>
	/// キャンセルコマンド
	/// </summary>
	public ReactiveCommand CancelCommand {
		get;
	}
}

/// <summary>
/// ダイアログの処理結果を表します。
/// </summary>
public enum DialogResult {
	/// <summary>未確定</summary>
	None,

	/// <summary>確定</summary>
	Confirmed,

	/// <summary>キャンセル</summary>
	Canceled
}