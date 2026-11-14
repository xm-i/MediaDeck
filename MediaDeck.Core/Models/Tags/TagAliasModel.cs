using MediaDeck.Common.Base;
using MediaDeck.Common.Utilities;
using MediaDeck.Composition.Interfaces.Tags;
using MediaDeck.Composition.Tables;

namespace MediaDeck.Core.Models.Tags;

/// <summary>
/// タグ別名のモデルクラス
/// </summary>
public class TagAliasModel : ModelBase, ITagAliasModel {
	private string _alias = string.Empty;
	private string? _ruby;

	/// <summary>
	/// デフォルトコンストラクタ。空のタグ別名モデルを初期化します。
	/// </summary>
	public TagAliasModel() {
	}

	/// <summary>
	/// エンティティからタグ別名モデルを初期化します。
	/// </summary>
	/// <param name="tagAlias">タグ別名エンティティ</param>
	public TagAliasModel(TagAlias tagAlias) {
		this.TagAliasId = tagAlias.TagAliasId;
		this.TagId = tagAlias.TagId;
		this._alias = tagAlias.Alias;
		this._ruby = tagAlias.Ruby;
		this.UpdateRomaji();
	}

	/// <inheritdoc />
	public int TagAliasId {
		get;
		set;
	}

	/// <inheritdoc />
	public int TagId {
		get;
		set;
	}

	/// <inheritdoc />
	public string Alias {
		get {
			return this._alias;
		}
		set {
			this._alias = value;
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
	public string Romaji {
		get;
		set;
	} = string.Empty;

	/// <summary>
	/// 現在の読み仮名または別名からローマ字表記を再計算します。
	/// </summary>
	private void UpdateRomaji() {
		var source = string.IsNullOrEmpty(this._ruby) ? this._alias : this._ruby;
		this.Romaji = source.KatakanaToHiragana().HiraganaToRomaji();
	}
}