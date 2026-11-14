using MediaDeck.Common.Base;
using MediaDeck.Common.Extensions;
using MediaDeck.Composition.Interfaces.Tags;
using MediaDeck.Core.Models.Tags;

namespace MediaDeck.ViewModels.Tags;

/// <summary>
/// タグカテゴリーの ViewModel クラス。
/// </summary>
public class TagCategoryViewModel : ViewModelBase {
	private readonly ITagsManager _tagsManager;
	private bool _isSyncingFromModel;
	private bool _hasUnsavedChanges;

	/// <summary>
	/// <see cref="TagCategoryViewModel"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	/// <param name="tagCategory">タグカテゴリーモデル</param>
	/// <param name="tagsManager">タグマネージャー</param>
	/// <param name="tagModelFactory">タグモデルファクトリー</param>
	/// <param name="onTagMoved">配下タグが他カテゴリへ移動した際のコールバック</param>
	public TagCategoryViewModel(
		ITagCategoryModel tagCategory,
		ITagsManager tagsManager,
		ITagModelFactory tagModelFactory,
		Action<ITagModel>? onTagMoved = null) {
		this._tagsManager = tagsManager;
		this.Model = tagCategory;

		this.SelectedTag = new BindableReactiveProperty<TagViewModel?>().AddTo(this.CompositeDisposable);
		this.TagCategoryName = new BindableReactiveProperty<string>(tagCategory.TagCategoryName).AddTo(this.CompositeDisposable);
		this.Detail = new BindableReactiveProperty<string>(tagCategory.Detail).AddTo(this.CompositeDisposable);
		this.FilterText = new BindableReactiveProperty<string>(string.Empty).AddTo(this.CompositeDisposable);

		this.Tags = tagCategory.Tags.CreateViewWithDisposal(x => new TagViewModel(this, x, tagsManager, tagModelFactory, onTagMoved)).AddTo(this.CompositeDisposable);
		this.FilteredTags = this.Tags.ToNotifyCollectionChanged(SynchronizationContextCollectionEventDispatcher.Current).AddTo(this.CompositeDisposable);

		this.FilterText.ThrottleLast(TimeSpan.FromMilliseconds(300))
			.ObserveOnCurrentSynchronizationContext()
			.Subscribe(_ => {
				this.RefreshTagCandidateFilter();
			}).AddTo(this.CompositeDisposable);

		tagCategory.Changed?.Subscribe(_ => {
			this.SyncFromModel();
		}).AddTo(this.CompositeDisposable);

		if (this.IsDeletable) {
			var changedStream = this.TagCategoryName.Skip(1)
				.ToUnit()
				.Merge(this.Detail.Skip(1).ToUnit())
				.Where(_ => !this._isSyncingFromModel);

			changedStream.Subscribe(_ => {
				this._hasUnsavedChanges = true;
			}).AddTo(this.CompositeDisposable);

			changedStream.ThrottleLast(TimeSpan.FromMilliseconds(300))
				.ObserveOnCurrentSynchronizationContext()
				.SubscribeAwait(async (_, ct) => {
					await this.SaveChangesAsync();
				}, AwaitOperation.Sequential)
				.AddTo(this.CompositeDisposable);
		}
	}

	/// <summary>
	/// タグカテゴリーモデルを取得します。
	/// </summary>
	public ITagCategoryModel Model {
		get;
	}

	/// <summary>
	/// タグカテゴリーIDを取得します。
	/// </summary>
	public int? TagCategoryId {
		get {
			return this.Model.TagCategoryId;
		}
	}

	/// <summary>
	/// カテゴリー配下のタグビューを取得します。
	/// </summary>
	public ISynchronizedView<ITagModel, TagViewModel> Tags {
		get;
	}

	/// <summary>
	/// このカテゴリが削除可能かどうか（システム定義の未分類カテゴリ以外は削除可能）
	/// </summary>
	public bool IsDeletable {
		get {
			return this.Model != this._tagsManager.NoCategory;
		}
	}

	/// <summary>
	/// フィルタリング済みタグリストを取得します。
	/// </summary>
	public INotifyCollectionChangedSynchronizedViewList<TagViewModel> FilteredTags {
		get;
	}

	/// <summary>
	/// 選択されているタグ
	/// </summary>
	public BindableReactiveProperty<TagViewModel?> SelectedTag {
		get;
	}

	/// <summary>
	/// タグカテゴリー名を取得します。
	/// </summary>
	public BindableReactiveProperty<string> TagCategoryName {
		get;
	}

	/// <summary>
	/// タグカテゴリーの説明を取得します。
	/// </summary>
	public BindableReactiveProperty<string> Detail {
		get;
	}

	/// <summary>
	/// タグ検索フィルター文字列を取得します。
	/// </summary>
	public BindableReactiveProperty<string> FilterText {
		get;
	}

	private void SyncFromModel() {
		this._isSyncingFromModel = true;
		try {
			if (this.TagCategoryName.Value != this.Model.TagCategoryName) {
				this.TagCategoryName.Value = this.Model.TagCategoryName;
			}
			if (this.Detail.Value != this.Model.Detail) {
				this.Detail.Value = this.Model.Detail;
			}
		} finally {
			this._isSyncingFromModel = false;
		}
	}

	private async Task SaveChangesAsync() {
		if (!this.IsDeletable || !this._tagsManager.TagCategories.Contains(this.Model)) {
			this._hasUnsavedChanges = false;
			return;
		}

		this._hasUnsavedChanges = false;
		await this._tagsManager.UpdateTagCategoryAsync(this.Model, this.TagCategoryName.Value, this.Detail.Value);
	}

	private void RefreshTagCandidateFilter() {
		this.Tags.AttachFilter((tag, tagVm) => {
			var text = this.FilterText.Value ?? "";
			if (text.Length == 0) {
				tagVm.RepresentativeText.Value = null;
				return true;
			}
			var result = tag.MatchesFilter(text, out var repText);
			tagVm.RepresentativeText.Value = repText;
			return result;
		});
	}

	/// <inheritdoc />
	protected override void Dispose(bool disposing) {
		if (disposing) {
			if (this._hasUnsavedChanges && this.IsDeletable && this._tagsManager.TagCategories.Contains(this.Model)) {
				this._hasUnsavedChanges = false;
				_ = this._tagsManager.UpdateTagCategoryAsync(this.Model, this.TagCategoryName.Value, this.Detail.Value);
			}
		}
		base.Dispose(disposing);
	}
}