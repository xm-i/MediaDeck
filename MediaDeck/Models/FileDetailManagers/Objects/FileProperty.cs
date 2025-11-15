using System.Collections.Generic;

using MediaDeck.Utils.Objects;

namespace MediaDeck.Models.FileDetailManagers.Objects;

/// <param name="title">タイトル</param>
/// <param name="values">値と件数リスト</param>
public class FileProperty(string title, IEnumerable<ValueCountPair<string?>> values) {
	/// <summary>
	/// タイトル
	/// </summary>
	public string Title {
		get;
	} = title;

	/// <summary>
	/// 代表値と件数
	/// </summary>
	public ValueCountPair<string?> RepresentativeValue {
		get {
			return this.Values.First();
		}
	}

	/// <summary>
	/// 値と件数リスト
	/// </summary>
	public IEnumerable<ValueCountPair<string?>> Values {
		get;
	} = values;

	/// <summary>
	/// 複数の値が含まれているか
	/// </summary>
	public bool HasMultipleValues {
		get {
			return this.Values.Count() >= 2;
		}
	}
}
