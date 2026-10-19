using MediaDeck.Common.Base;
using MediaDeck.Common.Extensions;
using MediaDeck.Composition.Interfaces.Tags;

namespace MediaDeck.ViewModels.Tags;

/// <summary>
/// タグのViewModelクラス。
/// </summary>
[Inject(InjectServiceLifetime.Transient)]
public class TagViewModel : ViewModelBase {
	/// <summary>
	/// <see cref="TagViewModel"/> の新しいインスタンスを初期化します。
	/// </summary>
	/// <param name="parent">親となるタグカテゴリViewModel。</param>
	/// <param name="tag">対象のタグモデル。</param>
	/// <param name="tagsManager">タグマネージャー。</param>
	/// <param name="tagModelFactory">タグモデルファクトリー。</param>
	public TagViewModel(TagCategoryViewModel parent, ITagModel tag, ITagsManager tagsManager, ITagModelFactory tagModelFactory) {
		this.TagName = new BindableReactiveProperty<string>(tag.TagName).AddTo(this.CompositeDisposable);
		this.Ruby = new BindableReactiveProperty<string>(tag.Ruby ?? string.Empty).AddTo(this.CompositeDisposable);
		this.Detail = new BindableReactiveProperty<string>(tag.Detail).AddTo(this.CompositeDisposable);
		this.TagCategory = new BindableReactiveProperty<TagCategoryViewModel>(parent).AddTo(this.CompositeDisposable);
		this.RepresentativeText = new BindableReactiveProperty<string?>().AddTo(this.CompositeDisposable);
		this.AddTagAliasCommand = new ReactiveCommand().AddTo(this.CompositeDisposable);
		this.RemoveTagAliasCommand = new ReactiveCommand<TagAliasViewModel>().AddTo(this.CompositeDisposable);

		this.Model = tag;
		this.UsageCount = tag.UsageCount.ToBindableReactiveProperty().AddTo(this.CompositeDisposable);
		this._tagAliases.AddRange(tag.TagAliases.Select(x => new TagAliasViewModel(x, this)));
		this.TagAliases = this._tagAliases.ToNotifyCollectionChanged(SynchronizationContextCollectionEventDispatcher.Current).AddTo(this.CompositeDisposable);

		this.RemoveTagAliasCommand.Subscribe(x => {
			if (this._tagAliases.Remove(x)) {
				x.Dispose();
			}
		}).AddTo(this.CompositeDisposable);
		this.AddTagAliasCommand.Subscribe(_ => {
			this._tagAliases.Add(new(tagModelFactory.CreateAlias(), this));
		}).AddTo(this.CompositeDisposable);

		this.TagName
			.ToUnit()
			.Merge(this.Ruby.ToUnit())
			.Merge(this.Detail.ToUnit())
			.Merge(this._tagAliases.ObserveChanged().ToUnit())
			.Merge(this.TagCategory.ToUnit())
			.Subscribe(_ => {
				this._editedFlag = true;
			}).AddTo(this.CompositeDisposable);
		this._editedFlag = false;
	}

	private bool _editedFlag = false;
	private readonly ObservableList<TagAliasViewModel> _tagAliases = [];

	/// <summary>
	/// タグモデルを取得します。
	/// </summary>
	public ITagModel Model {
		get;
	}

	/// <summary>
	/// タグ名を取得します。
	/// </summary>
	public BindableReactiveProperty<string> TagName {
		get;
	}

	/// <summary>
	/// タグの読み仮名を取得します。
	/// </summary>
	public BindableReactiveProperty<string> Ruby {
		get;
	}

	/// <summary>
	/// タグの詳細説明を取得します。
	/// </summary>
	public BindableReactiveProperty<string> Detail {
		get;
	}

	/// <summary>
	/// 所属するタグカテゴリを取得します。
	/// </summary>
	public BindableReactiveProperty<TagCategoryViewModel> TagCategory {
		get;
	}

	/// <summary>
	/// タグ別名（エイリアス）のリストを取得します。
	/// </summary>
	public INotifyCollectionChangedSynchronizedViewList<TagAliasViewModel> TagAliases {
		get;
	}

	/// <summary>
	/// ViewModelの変更内容をModelに反映します。
	/// </summary>
	public void SyncToModel() {
		if (!this._editedFlag) {
			return;
		}
		this.Model.TagName = this.TagName.Value;
		this.Model.Ruby = string.IsNullOrEmpty(this.Ruby.Value) ? null : this.Ruby.Value;
		this.Model.Detail = this.Detail.Value;
		this.Model.TagCategoryId = this.TagCategory.Value.TagCategoryId;
		this.Model.TagCategory = this.TagCategory.Value.Model;
		this.Model.TagAliases = this.TagAliases.Select(x => {
			var alias = x.Model;
			alias.Alias = x.Alias.Value;
			alias.Ruby = string.IsNullOrEmpty(x.Ruby.Value) ? null : x.Ruby.Value;
			return alias;
		}).ToList();
		this._editedFlag = false;
	}

	/// <summary>
	/// タグ別名を追加するコマンドを取得します。
	/// </summary>
	public ReactiveCommand AddTagAliasCommand {
		get;
	}

	/// <summary>
	/// タグ別名を削除するコマンドを取得します。
	/// </summary>
	public ReactiveCommand<TagAliasViewModel> RemoveTagAliasCommand {
		get;
	}

	/// <summary>
	/// 検索サジェスト等で使用される代表テキストを取得します。
	/// </summary>
	public BindableReactiveProperty<string?> RepresentativeText {
		get;
	}

	/// <summary>
	/// タグの使用回数を取得します。
	/// </summary>
	public BindableReactiveProperty<int> UsageCount {
		get;
	}

	/// <summary>
	/// 編集済みフラグを立てます。
	/// </summary>
	public void MarkAsEdited() {
		this._editedFlag = true;
	}

	/// <inheritdoc/>
	protected override void Dispose(bool disposing) {
		if (disposing) {
			foreach (var alias in this._tagAliases) {
				alias.Dispose();
			}
			this._tagAliases.Clear();
		}
		base.Dispose(disposing);
	}
}