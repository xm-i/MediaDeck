using System.ComponentModel;

namespace MediaDeck.ViewModels.Sort;

/// <summary>
/// ソート方向の選択肢を表すモデルクラス。
/// </summary>
public sealed class SortDirectionOption {
	/// <summary>
	/// <see cref="SortDirectionOption"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	/// <param name="direction">ソート方向。</param>
	/// <param name="displayName">表示名。</param>
	/// <param name="iconGlyph">アイコングリフ。</param>
	public SortDirectionOption(ListSortDirection direction, string displayName, string iconGlyph) {
		this.Direction = direction;
		this.DisplayName = displayName;
		this.IconGlyph = iconGlyph;
	}

	/// <summary>
	/// ソート方向を取得します。
	/// </summary>
	public ListSortDirection Direction {
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