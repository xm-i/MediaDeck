using System.Diagnostics.CodeAnalysis;
using MediaDeck.Composition.Interfaces.Tags;
using MediaDeck.Database.Tables;
using R3.JsonConfig.Attributes;

namespace MediaDeck.Core.Models.Tags;

/// <summary>
/// タグカテゴリーのモデルクラス
/// </summary>
[GenerateR3JsonConfigDto]
[JsonConfigDerivedType("tagCategory")]
[Inject(InjectServiceLifetime.Transient, typeof(ITagCategoryModel))]
[Inject(InjectServiceLifetime.Transient)]
public class TagCategoryModel : ITagCategoryModel {
	private int? _tagCategoryId;
	private string? _tagCategoryName;
	private string? _detail;
	private readonly ObservableList<ITagModel> _tags = [];
	private bool _isInitialized;

	public TagCategoryModel() {
	}

	public IReadOnlyObservableList<ITagModel> Tags {
		get {
			return this._tags;
		}
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

	[MemberNotNull(nameof(_tagCategoryName), nameof(_detail))]
	public void Initialize(TagCategory? tagCategory, ITagModelFactory factory) {
		if (tagCategory != null) {
			this._tagCategoryId = tagCategory.TagCategoryId;
			this._tagCategoryName = tagCategory.TagCategoryName;
			this._detail = tagCategory.Detail;
			this._tags.Clear();
			this._tags.AddRange(tagCategory.Tags.OrderByDescending(x => x.MediaItemTags.Count).Select(t => factory.Create(t, this)));
		} else {
			this._tagCategoryId = null;
			this._tagCategoryName = "未設定";
			this._detail = "カテゴリーが設定されていないタグ";
			this._tags.Clear();
		}
		this._isInitialized = true;
		this.IsDirty = false;
	}

	/// <summary>
	/// 変更フラグ
	/// </summary>
	public bool IsDirty {
		get;
		set;
	}

	/// <summary>
	/// タグカテゴリーID
	/// </summary>
	public int? TagCategoryId {
		get {
			if (!this._isInitialized) {
				throw new InvalidOperationException($"{nameof(this.TagCategoryId)} is not initialized.");
			}
			return this._tagCategoryId;
		}

		set {
			if (this._tagCategoryId != value) {
				this.IsDirty = true;
			}
			this._tagCategoryId = value;
			this._isInitialized = true;
		}
	}

	/// <summary>
	/// タグカテゴリー名
	/// </summary>
	public string TagCategoryName {
		get {
			return this._tagCategoryName ?? throw new InvalidOperationException($"{nameof(this.TagCategoryName)} is not initialized.");
		}

		[MemberNotNull(nameof(_tagCategoryName))]
		set {
			if (this._tagCategoryName != value) {
				this.IsDirty = true;
			}
			this._tagCategoryName = value;
			this._isInitialized = true;
		}
	}

	/// <summary>
	/// タグカテゴリーの説明
	/// </summary>
	public string Detail {
		get {
			return this._detail ?? throw new InvalidOperationException($"{nameof(this.Detail)} is not initialized.");
		}

		[MemberNotNull(nameof(_detail))]
		set {
			if (this._detail != value) {
				this.IsDirty = true;
			}
			this._detail = value;
			this._isInitialized = true;
		}
	}
}