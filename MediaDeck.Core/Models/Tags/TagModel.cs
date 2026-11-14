using MediaDeck.Common.Base;
using MediaDeck.Common.Utilities;
using MediaDeck.Composition.Interfaces.Tags;
using MediaDeck.Composition.Tables;

namespace MediaDeck.Core.Models.Tags;

/// <summary>
/// タグのモデルクラス
/// </summary>
public class TagModel : ModelBase, ITagModel {
	private readonly Subject<Unit> _changed;
	private string _tagName = string.Empty;
	private string? _ruby;

	/// <summary>
	/// デフォルトコンストラクタ。空のタグモデルを初期化します。
	/// </summary>
	public TagModel() {
		this._changed = new Subject<Unit>().AddTo(this.CompositeDisposable);
		this.UsageCount = new ReactiveProperty<int>(0).AddTo(this.CompositeDisposable);
		this.TagCategory = new TagCategoryModel().AddTo(this.CompositeDisposable);
	}

	/// <summary>
	/// エンティティからタグモデルを初期化します。
	/// </summary>
	/// <param name="tag">タグエンティティ</param>
	/// <param name="category">所属するカテゴリーモデル</param>
	/// <param name="factory">タグモデルファクトリー</param>
	public TagModel(Tag tag, ITagCategoryModel category, ITagModelFactory? factory = null) {
		this._changed = new Subject<Unit>().AddTo(this.CompositeDisposable);
		this.UsageCount = new ReactiveProperty<int>(tag.MediaItemTags.Count).AddTo(this.CompositeDisposable);

		this.TagId = tag.TagId;
		this.TagCategoryId = tag.TagCategoryId;
		this.TagCategory = category;
		this._tagName = tag.TagName;
		this._ruby = tag.Ruby;
		this.Detail = tag.Detail;
		this.UpdateRomaji();
		this.TagAliases = factory != null
			? [.. tag.TagAliases.Select(factory.CreateAlias)]
			: [.. tag.TagAliases.Select(x => (ITagAliasModel)new TagAliasModel(x))];
	}

	/// <inheritdoc />
	public int TagId {
		get;
		set;
	}

	/// <inheritdoc />
	public int? TagCategoryId {
		get;
		set;
	}

	/// <inheritdoc />
	public ITagCategoryModel TagCategory {
		get;
		set;
	}

	/// <inheritdoc />
	public string TagName {
		get {
			return this._tagName;
		}
		set {
			this._tagName = value;
			this.UpdateRomaji();
		}
	}

	/// <inheritdoc />
	public string? Ruby {
		get {
			return this._ruby;
		}
		set {
			this._ruby = value;
			this.UpdateRomaji();
		}
	}

	/// <inheritdoc />
	public string Detail {
		get;
		set;
	} = string.Empty;

	/// <inheritdoc />
	public string Romaji {
		get;
		set;
	} = string.Empty;

	/// <inheritdoc />
	public List<ITagAliasModel> TagAliases {
		get;
		set;
	} = [];

	/// <inheritdoc />
	public ReactiveProperty<int> UsageCount {
		get;
	}

	/// <inheritdoc />
	public Observable<Unit> Changed {
		get {
			return this._changed.AsObservable();
		}
	}

	/// <inheritdoc />
	public void NotifyChanged() {
		this._changed.OnNext(Unit.Default);
	}

	/// <summary>
	/// 現在の読み仮名またはタグ名からローマ字表記を再計算します。
	/// </summary>
	private void UpdateRomaji() {
		var source = string.IsNullOrEmpty(this._ruby) ? this._tagName : this._ruby;
		this.Romaji = source.KatakanaToHiragana().HiraganaToRomaji();
	}
}