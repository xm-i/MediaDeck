namespace MediaDeck.Composition.Tables;

/// <summary>
/// データベースバージョン
/// </summary>
public class DbVersion {
	/// <summary>
	/// ID
	/// </summary>
	public long Id {
		get;
		set;
	}

	/// <summary>
	/// バージョン
	/// </summary>
	public int Version {
		get;
		set;
	}
}