using MediaDeck.Common.Base;
using MediaDeck.Composition.Interfaces;
using MediaDeck.Composition.Interfaces.Tags;
using MediaDeck.Composition.Tables;

namespace MediaDeck.Core.Models.Tags;

/// <summary>
/// タグカテゴリーのモデルクラス
/// </summary>
public class TagCategoryModel : ModelBase, ITagCategoryModel {
	private readonly ObservableList<ITagModel> _tags = [];
	private readonly Subject<Unit> _changed;
	private string _tagCategoryName = string.Empty;
	private string _detail = string.Empty;

	/// <summary>
	/// <see cref="TagCategoryModel"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	/// <param name="tagCategory">タグカテゴリーエンティティ（未分類の場合は null）</param>
	/// <param name="factory">タグモデルファクトリー</param>
	/// <param name="stringProvider">文字列リソースプロバイダー</param>
	public TagCategoryModel(TagCategory? tagCategory = null, ITagModelFactory? factory = null, IStringProvider? stringProvider = null) {
		this._changed = new Subject<Unit>().AddTo(this.CompositeDisposable);

		if (tagCategory != null) {
			this.TagCategoryId = tagCategory.TagCategoryId;
			this._tagCategoryName = tagCategory.TagCategoryName;
			this._detail = tagCategory.Detail;
			if (factory != null) {
				this._tags.AddRange(tagCategory.Tags.OrderByDescending(x => x.MediaItemTags.Count).Select(t => factory.Create(t, this)));
			}
		} else {
			this.TagCategoryId = null;
			this._tagCategoryName = stringProvider?.GetString("TagCategory_Unassigned_Name") ?? "未設定";
			this._detail = stringProvider?.GetString("TagCategory_Unassigned_Detail") ?? "カテゴリーが設定されていないタグ";
		}
	}

	/// <inheritdoc />
	public Observable<Unit> Changed {
		get {
			return this._changed.AsObservable();
		}
	}

	/// <inheritdoc />
	public IReadOnlyObservableList<ITagModel> Tags {
		get {
			return this._tags;
		}
	}

	/// <inheritdoc />
	public int? TagCategoryId {
		get;
		set;
	}

	/// <inheritdoc />
	public string TagCategoryName {
		get {
			return this._tagCategoryName;
		}
		set {
			if (this._tagCategoryName != value) {
				this._tagCategoryName = value;
				this._changed.OnNext(Unit.Default);
			}
		}
	}

	/// <inheritdoc />
	public string Detail {
		get {
			return this._detail;
		}
		set {
			if (this._detail != value) {
				this._detail = value;
				this._changed.OnNext(Unit.Default);
			}
		}
	}

	/// <inheritdoc />
	public void NotifyChanged() {
		this._changed.OnNext(Unit.Default);
	}

	/// <inheritdoc />
	public void AddTag(ITagModel tag) {
		this._tags.Add(tag);
	}

	/// <inheritdoc />
	public void RemoveTag(ITagModel tag) {
		this._tags.Remove(tag);
	}

	/// <inheritdoc />
	public void AddTagRange(IEnumerable<ITagModel> tags) {
		this._tags.AddRange(tags);
	}

	/// <inheritdoc />
	public void ClearTags() {
		this._tags.Clear();
	}
}