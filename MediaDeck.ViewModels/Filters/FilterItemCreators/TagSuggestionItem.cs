namespace MediaDeck.ViewModels.Filters.FilterItemCreators;

/// <summary>
/// タグフィルター入力欄のサジェスト候補を表すモデルクラス。
/// </summary>
public sealed class TagSuggestionItem {
	/// <summary>
	/// <see cref="TagSuggestionItem"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	/// <param name="tagName">タグ名。</param>
	/// <param name="usageCount">使用回数。</param>
	/// <param name="aliasText">一致したエイリアス表示テキスト。</param>
	public TagSuggestionItem(string tagName, int usageCount, string? aliasText) {
		this.TagName = tagName;
		this.UsageCount = usageCount;
		this.AliasText = aliasText ?? string.Empty;
		this.HasAliasText = !string.IsNullOrEmpty(aliasText);
	}

	/// <summary>
	/// タグ名を取得します。
	/// </summary>
	public string TagName {
		get;
	}

	/// <summary>
	/// タグの使用回数を取得します。
	/// </summary>
	public int UsageCount {
		get;
	}

	/// <summary>
	/// 一致したエイリアス表示テキストを取得します。
	/// </summary>
	public string AliasText {
		get;
	}

	/// <summary>
	/// エイリアス表示テキストが存在するかどうかを取得します。
	/// </summary>
	public bool HasAliasText {
		get;
	}

	/// <inheritdoc />
	public override string ToString() {
		return this.TagName;
	}
}