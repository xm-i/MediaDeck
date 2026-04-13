using System.Diagnostics.CodeAnalysis;
using MediaDeck.Common.Utilities;
using MediaDeck.Composition.Interfaces.Tags;
using MediaDeck.Database.Tables;
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
	private bool _isInitialized;

	public TagModel() {
	}

	[MemberNotNull(nameof(_tagId), nameof(_tagName), nameof(_detail), nameof(_romaji), nameof(_tagAliases), nameof(_tagCategory))]
	public void Initialize(Tag tag, ITagCategoryModel category, ITagModelFactory factory) {
		this._tagId = tag.TagId;
		this._tagCategoryId = tag.TagCategoryId;
		this._tagCategory = category;
		this._tagName = tag.TagName;
		this._detail = tag.Detail;
		this._romaji = tag.TagName.KatakanaToHiragana().HiraganaToRomaji();
		this.UsageCount.Value = tag.MediaFileTags.Count;
		this._tagAliases = [.. tag.TagAliases.Select(factory.CreateAlias)];
		this._isInitialized = true;
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
			this._isInitialized = true;
		}
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
	/// タグカテゴリー
	/// </summary>
	public ITagCategoryModel TagCategory {
		get {
			return this._tagCategory ?? throw new InvalidOperationException($"{nameof(this.TagCategory)} is not initialized.");
		}

		[MemberNotNull(nameof(_tagCategory))]
		set {
			this._tagCategory = value;
			this._isInitialized = true;
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
			this._isInitialized = true;
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
			this._isInitialized = true;
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
			this._isInitialized = true;
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
			this._isInitialized = true;
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