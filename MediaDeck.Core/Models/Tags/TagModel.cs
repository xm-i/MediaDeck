using System.Diagnostics.CodeAnalysis;
using MediaDeck.Common.Utilities;
using MediaDeck.Composition.Interfaces.Tags;
using MediaDeck.Core.Models.Tags;
using MediaDeck.Database.Tables;
using R3;
using R3.JsonConfig.Attributes;

namespace MediaDeck.Core.Models.Tags;

/// <summary>
/// タグのモデルクラス
/// </summary>
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
		this.TagCategory = category ?? factory.CreateCategory(tag.TagCategory);
		this.TagName = tag.TagName;
		this.Detail = tag.Detail;
		this.Romaji = tag.TagName.KatakanaToHiragana().HiraganaToRomaji();
		this.UsageCount.Value = tag.MediaFileTags.Count;
		this.TagAliases = [.. tag.TagAliases.Select(factory.CreateAlias)];
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
	/// タグカテゴリー
	/// </summary>
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
	/// タグの説明
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

	/// <summary>
	/// ローマ字表記
	/// </summary>
	public string Romaji {
		get {
			return this._romaji ?? throw new InvalidOperationException($"{nameof(this.Romaji)} is not initialized.");
		}

		[MemberNotNull(nameof(_romaji))]
		set {
			this._romaji = value;
		}
	}

	/// <summary>
	/// タグ別名のリスト
	/// </summary>
	public List<ITagAliasModel> TagAliases {
		get {
			return this._tagAliases ?? throw new InvalidOperationException($"{nameof(this.TagAliases)} is not initialized.");
		}

		[MemberNotNull(nameof(_tagAliases))]
		set {
			this._tagAliases = value;
		}
	}

	/// <summary>
	/// 代表テキスト
	/// </summary>
	public ReactiveProperty<string?> RepresentativeText {
		get;
		set;
	} = new();

	/// <summary>
	/// 使用回数
	/// </summary>
	public ReactiveProperty<int> UsageCount {
		get;
		set;
	} = new();
}