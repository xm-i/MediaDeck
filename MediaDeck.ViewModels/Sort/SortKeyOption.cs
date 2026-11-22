using MediaDeck.Composition.Enum;

namespace MediaDeck.ViewModels.Sort;

/// <summary>
/// ソートキーの選択肢を表すモデルクラス。
/// </summary>
public sealed class SortKeyOption {
	/// <summary>
	/// <see cref="SortKeyOption"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	/// <param name="key">ソートキー。</param>
	/// <param name="displayName">表示名。</param>
	/// <param name="iconGlyph">アイコングリフ。</param>
	public SortKeyOption(SortItemKey key, string displayName, string iconGlyph) {
		this.Key = key;
		this.DisplayName = displayName;
		this.IconGlyph = iconGlyph;
	}

	/// <summary>
	/// ソートキーを取得します。
	/// </summary>
	public SortItemKey Key {
		get;
	}

	/// <summary>
	/// 表示名を取得します。
	/// </summary>
	public string DisplayName {
		get;
	}

	/// <summary>
	/// Segoe Fluent Icons のグリフ文字列を取得します。
	/// </summary>
	public string IconGlyph {
		get;
	}
}