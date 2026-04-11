using MediaDeck.Composition.Interfaces.Tags;
using MediaDeck.Database.Tables;
using R3.JsonConfig.Attributes;

namespace MediaDeck.Composition.Interfaces.Tags;

/// <summary>
/// タグ別名のモデルクラスのインターフェース
/// </summary>
[GenerateR3JsonConfigDto]
public interface ITagAliasModel {
	/// <summary>
	/// タグ別名ID
	/// </summary>
	public int TagAliasId {
		get;
		set;
	}

	/// <summary>
	/// タグID
	/// </summary>
	public int TagId {
		get;
		set;
	}

	/// <summary>
	/// 別名
	/// </summary>
	public string Alias {
		get;
		set;
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
		get;
		set;
	}

	public void Initialize(TagAlias tagAlias);
}