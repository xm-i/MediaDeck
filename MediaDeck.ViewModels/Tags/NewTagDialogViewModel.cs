using MediaDeck.Common.Base;
using MediaDeck.Composition.Interfaces.Tags;

namespace MediaDeck.ViewModels.Tags;

[Inject(InjectServiceLifetime.Transient)]
public class NewTagDialogViewModel : ViewModelBase {
	/// <summary>
	/// <see cref="NewTagDialogViewModel"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	/// <param name="tagsManager">タグマネージャー</param>
	/// <param name="tagModelFactory">タグモデルファクトリー</param>
	public NewTagDialogViewModel(ITagsManager tagsManager, ITagModelFactory tagModelFactory) {
		this.TagName = new BindableReactiveProperty<string>("").AddTo(this.CompositeDisposable);
		this.Ruby = new BindableReactiveProperty<string>("").AddTo(this.CompositeDisposable);
		this.Detail = new BindableReactiveProperty<string>("").AddTo(this.CompositeDisposable);
		this.SelectedCategory = new BindableReactiveProperty<TagCategoryViewModel?>().AddTo(this.CompositeDisposable);
		this.Result = new BindableReactiveProperty<DialogResult>(DialogResult.None).AddTo(this.CompositeDisposable);
		this.CreatedTag = new BindableReactiveProperty<ITagModel?>().AddTo(this.CompositeDisposable);
		this.ConfirmCommand = new ReactiveCommand().AddTo(this.CompositeDisposable);
		this.CancelCommand = new ReactiveCommand().AddTo(this.CompositeDisposable);

		this.TagCategories = tagsManager.TagCategories.Select(x => new TagCategoryViewModel(x, tagsManager, tagModelFactory).AddTo(this.CompositeDisposable)).ToArray();
		this.SelectedCategory.Value = this.TagCategories.FirstOrDefault();
		this.ConfirmCommand.SubscribeAwait(async (_, ct) => {
			// タグを作成
			this.CreatedTag.Value = await tagsManager.CreateTagImmediatelyAsync(this.SelectedCategory.Value?.Model.TagCategoryId,
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
	public BindableReactiveProperty<TagCategoryViewModel?> SelectedCategory {
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
	public TagCategoryViewModel[] TagCategories {
		get;
		private set;
	} = [];

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

public enum DialogResult {
	None,
	Confirmed,
	Canceled
}