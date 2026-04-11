using System.Diagnostics.CodeAnalysis;
using MediaDeck.Common.Utilities;
using MediaDeck.Composition.Interfaces.Tags;
using MediaDeck.Core.Models.Tags;
using MediaDeck.Database.Tables;
using R3.JsonConfig.Attributes;

namespace MediaDeck.Core.Models.Tags;

/// <summary>
/// タグ別名のモデルクラス
/// </summary>
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
}