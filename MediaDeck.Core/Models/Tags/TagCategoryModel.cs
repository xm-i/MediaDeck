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
	private ObservableList<ITagModel>? _tags;
	private bool _isInitialized;

	public TagCategoryModel() {
	}

	public ObservableList<ITagModel> Tags {
		get {
			return this._tags ??= [];
		}
	}

	[MemberNotNull(nameof(_tagCategoryName), nameof(_detail))]
	public void Initialize(TagCategory? tagCategory, ITagModelFactory factory) {
		if (tagCategory != null) {
			this._tagCategoryId = tagCategory.TagCategoryId;
			this._tagCategoryName = tagCategory.TagCategoryName;
			this._detail = tagCategory.Detail;
			this.Tags.Clear();
			this.Tags.AddRange(tagCategory.Tags.OrderByDescending(x => x.MediaFileTags.Count).Select(t => factory.Create(t, this)));
		} else {
			this._tagCategoryId = null;
			this._tagCategoryName = "未設定";
			this._detail = "カテゴリーが設定されていないタグ";
			this.Tags.Clear();
		}
		this._isInitialized = true;
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
			this._detail = value;
			this._isInitialized = true;
		}
	}
}