namespace MediaDeck.ViewModels.Filters.FilterItemCreators;

/// <summary>
/// メディアタイプフィルターの選択肢を表すモデルクラス。
/// </summary>
public sealed class MediaTypeOption {
	/// <summary>
	/// <see cref="MediaTypeOption"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	/// <param name="displayString">表示名。</param>
	/// <param name="value">動画であるかどうか（<see langword="true"/>: 動画、<see langword="false"/>: 画像）。</param>
	/// <param name="iconGlyph">アイコングリフ。</param>
	public MediaTypeOption(string displayString, bool value, string iconGlyph) {
		this.DisplayString = displayString;
		this.Value = value;
		this.IconGlyph = iconGlyph;
	}

	/// <summary>
	/// 表示名を取得します。
	/// </summary>
	public string DisplayString {
		get;
	}

	/// <summary>
	/// 動画であるかどうかを取得します。
	/// </summary>
	public bool Value {
		get;
	}

	/// <summary>
	/// Segoe Fluent Icons のグリフ文字列を取得します。
	/// </summary>
	public string IconGlyph {
		get;
	}
}