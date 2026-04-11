using System.Diagnostics.CodeAnalysis;
using MediaDeck.Common.Utilities;
using MediaDeck.Composition.Interfaces.Files;
using MediaDeck.Database.Tables;
using R3.JsonConfig.Attributes;

namespace MediaDeck.Core.Models.Files;

[GenerateR3JsonConfigDto]
[JsonConfigDerivedType("tag")]
[Inject(InjectServiceLifetime.Transient, typeof(ITagModel))]
[Inject(InjectServiceLifetime.Transient)] // TagModel 自身としても登録
public class TagModel : ITagModel {
	private int? _tagId;
	private int? _tagCategoryId;
	private ITagCategoryModel? _tagCategory;
	private string? _tagName;
	private string? _detail;
	private string? _romaji;
	private List<ITagAliasModel>? _tagAliases;

	public TagModel() {
	}

	[MemberNotNull(nameof(_tagId), nameof(_tagCategoryId), nameof(_tagCategory), nameof(_tagName), nameof(_detail), nameof(_romaji), nameof(_tagAliases))]
	public void Initialize(Tag tag, ITagCategoryModel? category, ITagModelFactory factory) {
		this.TagId = tag.TagId;
		this.TagCategoryId = tag.TagCategoryId;
		this.TagCategory = category ?? factory.Create(tag.TagCategory);
		this.TagName = tag.TagName;
		this.Detail = tag.Detail;
		this.Romaji = tag.TagName.KatakanaToHiragana().HiraganaToRomaji();
		this.UsageCount.Value = tag.MediaFileTags.Count;
		this.TagAliases = [.. tag.TagAliases.Select(factory.Create)];
	}

	/// <summary>
	/// タグID
	/// </summary>
	public int TagId {
		get {
			return this._tagId ?? throw new InvalidOperationException($"{nameof(this.TagId)} is not initialized.");
		}

		[MemberNotNull(nameof(_tagId))]
		set {
			this._tagId = value;
		}
	}

	/// <summary>
	/// タグ分類
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

	public ITagCategoryModel TagCategory {
		get {
			return this._tagCategory ?? throw new InvalidOperationException($"{nameof(this.TagCategory)} is not initialized.");
		}

		[MemberNotNull(nameof(_tagCategory))]
		set {
			this._tagCategory = value;
		}
	}

	/// <summary>
	/// タグ名
	/// </summary>
	public string TagName {
		get {
			return this._tagName ?? throw new InvalidOperationException($"{nameof(this.TagName)} is not initialized.");
		}

		[MemberNotNull(nameof(_tagName))]
		set {
			this._tagName = value;
		}
	}

	/// <summary>
	/// タグ説明
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

	public string Romaji {
		get {
			return this._romaji ?? throw new InvalidOperationException($"{nameof(this.Romaji)} is not initialized.");
		}

		[MemberNotNull(nameof(_romaji))]
		set {
			this._romaji = value;
		}
	}

	public List<ITagAliasModel> TagAliases {
		get {
			return this._tagAliases ?? throw new InvalidOperationException($"{nameof(this.TagAliases)} is not initialized.");
		}

		[MemberNotNull(nameof(_tagAliases))]
		set {
			this._tagAliases = value;
		}
	}

	public ReactiveProperty<string?> RepresentativeText {
		get;
		set;
	} = new();

	public ReactiveProperty<int> UsageCount {
		get;
		set;
	} = new();
}

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
	/// タグ分類ID
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
	/// タグ分類名
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
	/// タグ分類の説明
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

[GenerateR3JsonConfigDto]
[JsonConfigDerivedType("tagAlias")]
[Inject(InjectServiceLifetime.Transient, typeof(ITagAliasModel))]
[Inject(InjectServiceLifetime.Transient)]
public class TagAliasModel : ITagAliasModel {
	private int? _tagAliasId;
	private int? _tagId;
	private string? _alias;
	private string? _romaji;

	public TagAliasModel() {
	}

	[MemberNotNull(nameof(_tagAliasId), nameof(_tagId), nameof(_alias), nameof(_romaji))]
	public void Initialize(TagAlias tagAlias) {
		this.TagAliasId = tagAlias.TagAliasId;
		this.TagId = tagAlias.TagId;
		this.Alias = tagAlias.Alias;
		this.Ruby = tagAlias.Ruby;
		this.Romaji = (tagAlias.Ruby ?? tagAlias.Alias.KatakanaToHiragana()).HiraganaToRomaji();
	}

	/// <summary>
	/// タグ別名ID
	/// </summary>
	public int TagAliasId {
		get {
			return this._tagAliasId ?? throw new InvalidOperationException($"{nameof(this.TagAliasId)} is not initialized.");
		}

		[MemberNotNull(nameof(_tagAliasId))]
		set {
			this._tagAliasId = value;
		}
	}

	/// <summary>
	/// タグID
	/// </summary>
	public int TagId {
		get {
			return this._tagId ?? throw new InvalidOperationException($"{nameof(this.TagId)} is not initialized.");
		}

		[MemberNotNull(nameof(_tagId))]
		set {
			this._tagId = value;
		}
	}

	/// <summary>
	/// 別名
	/// </summary>
	public string Alias {
		get {
			return this._alias ?? throw new InvalidOperationException($"{nameof(this.Alias)} is not initialized.");
		}

		[MemberNotNull(nameof(_alias))]
		set {
			this._alias = value;
		}
	}

	/// <summary>
	/// 読み仮名
	/// </summary>
	public string? Ruby {
		get;
		set;
	}

	public string Romaji {
		get {
			return this._romaji ?? throw new InvalidOperationException($"{nameof(this.Romaji)} is not initialized.");
		}

		[MemberNotNull(nameof(_romaji))]
		set {
			this._romaji = value;
		}
	}
}