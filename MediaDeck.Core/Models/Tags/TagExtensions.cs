using MediaDeck.Composition.Interfaces.Tags;

namespace MediaDeck.Core.Models.Tags;

/// <summary>
/// タグモデルの拡張メソッドを提供します。
/// </summary>
public static class TagExtensions {
	/// <summary>
	/// タグが指定された検索テキストのフィルタ条件に一致するかどうかを判定します。
	/// </summary>
	/// <param name="tag">判定対象のタグ</param>
	/// <param name="text">検索テキスト</param>
	/// <param name="representativeText">一致したエイリアステキスト（直接一致または不一致の場合はnull）</param>
	/// <returns>フィルタ条件に一致する場合は true、それ以外は false</returns>
	public static bool MatchesFilter(this ITagModel tag, string text, out string? representativeText) {
		representativeText = null;
		if (string.IsNullOrEmpty(text)) {
			return false;
		}

		if (tag.TagName.Contains(text) ||
			(tag.Ruby?.Contains(text) ?? false) ||
			(tag.Romaji?.Contains(text, StringComparison.CurrentCultureIgnoreCase) ?? false)) {
			return true;
		}

		var result = tag.TagAliases.FirstOrDefault(x =>
			x.Alias.Contains(text, StringComparison.CurrentCultureIgnoreCase) ||
			(x.Ruby?.Contains(text) ?? false) ||
			(x.Romaji?.Contains(text, StringComparison.CurrentCultureIgnoreCase) ?? false));

		representativeText = result?.Alias;
		return result != null;
	}
}