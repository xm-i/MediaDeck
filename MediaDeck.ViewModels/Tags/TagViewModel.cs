using MediaDeck.Common.Base;
using MediaDeck.Common.Extensions;
using MediaDeck.Composition.Interfaces.Tags;

namespace MediaDeck.ViewModels.Tags;

/// <summary>
/// タグの ViewModel クラス。
/// </summary>
[Inject(InjectServiceLifetime.Transient)]
public class TagViewModel : ViewModelBase {
	private readonly ITagsManager _tagsManager;
	private readonly TagCategoryViewModel? _parentCategory;
	private readonly Action<ITagModel>? _onTagMoved;
	private readonly ObservableList<TagAliasViewModel> _tagAliases = [];
	private readonly Subject<Unit> _saveTrigger;
	private bool _isSyncingFromModel;
	private bool _hasUnsavedChanges;

	/// <summary>
	/// <see cref="TagViewModel"/> の新しいインスタンスを初期化します。
	/// </summary>
	/// <param name="parent">親となるタグカテゴリ ViewModel（表示専用の場合は null）。</param>
	/// <param name="tag">対象のタグモデル。</param>
	/// <param name="tagsManager">タグマネージャー。</param>
	/// <param name="tagModelFactory">タグモデルファクトリー。</param>
	/// <param name="onTagMoved">カテゴリ移動完了時に呼び出されるコールバック。</param>
	public TagViewModel(
		TagCategoryViewModel? parent,
		ITagModel tag,
		ITagsManager tagsManager,
		ITagModelFactory tagModelFactory,
		Action<ITagModel>? onTagMoved = null) {
		this._parentCategory = parent;
		this._tagsManager = tagsManager;
		this._onTagMoved = onTagMoved;
		this.Model = tag;

		this.TagName = new BindableReactiveProperty<string>(tag.TagName).AddTo(this.CompositeDisposable);
		this.Ruby = new BindableReactiveProperty<string>(tag.Ruby ?? string.Empty).AddTo(this.CompositeDisposable);
		this.Detail = new BindableReactiveProperty<string>(tag.Detail).AddTo(this.CompositeDisposable);
		this.TagCategory = new BindableReactiveProperty<TagCategoryViewModel?>(parent).AddTo(this.CompositeDisposable);
		this.RepresentativeText = new BindableReactiveProperty<string?>().AddTo(this.CompositeDisposable);
		this.UsageCount = tag.UsageCount.ToBindableReactiveProperty().AddTo(this.CompositeDisposable);
		this.AddTagAliasCommand = new ReactiveCommand().AddTo(this.CompositeDisposable);
		this.RemoveTagAliasCommand = new ReactiveCommand<TagAliasViewModel>().AddTo(this.CompositeDisposable);
		this._saveTrigger = new Subject<Unit>().AddTo(this.CompositeDisposable);

		this._tagAliases.AddRange(tag.TagAliases.Select(x => new TagAliasViewModel(x, this)));
		this.TagAliases = this._tagAliases.ToNotifyCollectionChanged(SynchronizationContextCollectionEventDispatcher.Current).AddTo(this.CompositeDisposable);

		// モデルからの変更通知を ViewModel へ同期
		tag.Changed?.Subscribe(_ => {
			this.SyncFromModel();
		}).AddTo(this.CompositeDisposable);

		// 親カテゴリが存在する（TagManager で編集対象となっている）場合のみ自動保存を有効化
		if (parent != null) {
			this.TagName.Skip(1)
				.ToUnit()
				.Merge(this.Ruby.Skip(1).ToUnit())
				.Merge(this.Detail.Skip(1).ToUnit())
				.Where(_ => !this._isSyncingFromModel)
				.Subscribe(_ => this.RequestSave())
				.AddTo(this.CompositeDisposable);

			this._saveTrigger
				.ThrottleLast(TimeSpan.FromMilliseconds(300))
				.ObserveOnCurrentSynchronizationContext()
				.SubscribeAwait(async (_, ct) => {
					await this.SaveChangesAsync();
				}, AwaitOperation.Sequential)
				.AddTo(this.CompositeDisposable);

			this.TagCategory.Skip(1)
				.Where(x => x != null && !this._isSyncingFromModel)
				.SubscribeAwait(async (_, ct) => {
					await this.SaveChangesAsync();
				}, AwaitOperation.Sequential)
				.AddTo(this.CompositeDisposable);

			this.RemoveTagAliasCommand.SubscribeAwait(async (x, ct) => {
				if (this._tagAliases.Remove(x)) {
					x.Dispose();
					await this.SaveChangesAsync();
				}
			}, AwaitOperation.Sequential).AddTo(this.CompositeDisposable);

			this.AddTagAliasCommand.SubscribeAwait(async (_, ct) => {
				this._tagAliases.Add(new TagAliasViewModel(tagModelFactory.CreateAlias(), this));
				await this.SaveChangesAsync();
			}, AwaitOperation.Sequential).AddTo(this.CompositeDisposable);
		}
	}

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
	public BindableReactiveProperty<TagCategoryViewModel?> TagCategory {
		get;
	}

	/// <summary>
	/// タグ別名（エイリアス）のリストを取得します。
	/// </summary>
	public INotifyCollectionChangedSynchronizedViewList<TagAliasViewModel> TagAliases {
		get;
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
	/// テキスト等の変更に対するデバウンス保存を要求します。
	/// </summary>
	public void RequestSave() {
		if (this._isSyncingFromModel) {
			return;
		}
		this._hasUnsavedChanges = true;
		this._saveTrigger.OnNext(Unit.Default);
	}

	private void SyncFromModel() {
		this._isSyncingFromModel = true;
		try {
			if (this.TagName.Value != this.Model.TagName) {
				this.TagName.Value = this.Model.TagName;
			}
			var ruby = this.Model.Ruby ?? string.Empty;
			if (this.Ruby.Value != ruby) {
				this.Ruby.Value = ruby;
			}
			if (this.Detail.Value != this.Model.Detail) {
				this.Detail.Value = this.Model.Detail;
			}
		} finally {
			this._isSyncingFromModel = false;
		}
	}

	private async Task SaveChangesAsync() {
		if (!this._tagsManager.Tags.Contains(this.Model)) {
			this._hasUnsavedChanges = false;
			return;
		}

		var targetCategoryVm = this.TagCategory.Value ?? this._parentCategory;
		if (targetCategoryVm == null) {
			this._hasUnsavedChanges = false;
			return;
		}

		var targetCategoryModel = targetCategoryVm.Model;
		var categoryMoved = this._parentCategory != null && targetCategoryModel != this._parentCategory.Model;
		var tagName = this.TagName.Value;
		var ruby = string.IsNullOrEmpty(this.Ruby.Value) ? null : this.Ruby.Value;
		var detail = this.Detail.Value;
		var aliases = this._tagAliases.Select(x => x.Model).ToArray();

		this._hasUnsavedChanges = false;
		await this._tagsManager.UpdateTagAsync(this.Model, targetCategoryModel, tagName, ruby, detail, aliases);

		if (categoryMoved) {
			this._onTagMoved?.Invoke(this.Model);
		}
	}

	/// <inheritdoc/>
	protected override void Dispose(bool disposing) {
		if (disposing) {
			if (this._hasUnsavedChanges && this._tagsManager.Tags.Contains(this.Model)) {
				var targetCategoryModel = (this.TagCategory.Value ?? this._parentCategory)?.Model;
				if (targetCategoryModel != null) {
					this._hasUnsavedChanges = false;
					var tagName = this.TagName.Value;
					var ruby = string.IsNullOrEmpty(this.Ruby.Value) ? null : this.Ruby.Value;
					var detail = this.Detail.Value;
					var aliases = this._tagAliases.Select(x => x.Model).ToArray();
					_ = this._tagsManager.UpdateTagAsync(this.Model, targetCategoryModel, tagName, ruby, detail, aliases);
				}
			}

			foreach (var alias in this._tagAliases) {
				alias.Dispose();
			}
			this._tagAliases.Clear();
		}
		base.Dispose(disposing);
	}
}