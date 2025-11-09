namespace MediaDeck.Models.Files.Filter.FilterItemObjects;
/// <summary>
/// ファイル存在フィルターアイテムオブジェクト
/// </summary>
public class ExistsFilterItemObject : IFilterItemObject {
	/// <summary>
	/// 表示名
	/// </summary>
	public string DisplayName {
		get {
			return $"File {(this.Exists ? "exists" : "does not exist")}";
		}
	}
	/// <summary>
	/// ファイルが存在するか否か
	/// </summary>
	public bool Exists {
		get;
		set;
	}

	[Obsolete("for serialize")]
	public ExistsFilterItemObject() {
	}

	/// <summary>
	/// コンストラクタ
	/// </summary>
	/// <param name="exists">ファイルが存在するか否か</param>
	public ExistsFilterItemObject(bool exists) {
		this.Exists = exists;
	}
}