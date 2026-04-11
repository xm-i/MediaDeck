using System.Diagnostics.CodeAnalysis;
using MediaDeck.Composition.Interfaces.Tags;
using MediaDeck.Core.Models.Tags;
using MediaDeck.Database.Tables;
using ObservableCollections;
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

	public TagCategoryModel() {
	}

	public ObservableList<ITagModel> Tags {
		get {
			return this._tags ??= [];
		}
	}

	[MemberNotNull(nameof(_tagCategoryId), nameof(_tagCategoryName), nameof(_detail))]
	public void Initialize(TagCategory tagCategory, ITagModelFactory factory) {
		this.TagCategoryId = tagCategory.TagCategoryId;
		this.TagCategoryName = tagCategory.TagCategoryName;
		this.Detail = tagCategory.Detail;
		this.Tags.Clear();
		this.Tags.AddRange(tagCategory.Tags.OrderByDescending(x => x.MediaFileTags.Count).Select(t => factory.Create(t, this)));
	}

	/// <summary>
	/// タグカテゴリーID
	/// </summary>
	public int TagCategoryId {
		get {
			return this._tagCategoryId ?? throw new InvalidOperationException($"{nameof(this.TagCategoryId)} is not initialized.");
		}

		[MemberNotNull(nameof(_tagCategoryId))]
		set {
			this._tagCategoryId = value;
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
		}
	}
}