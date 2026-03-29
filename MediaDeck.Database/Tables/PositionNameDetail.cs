namespace MediaDeck.Database.Tables;

public class PositionNameDetail {
	/// <summary>
	/// 緯度
	/// </summary>
	public double Latitude {
		get;
		set;
	}

	/// <summary>
	/// 経度
	/// </summary>
	public double Longitude {
		get;
		set;
	}

	/// <summary>
	/// 名前の種類
	/// </summary>
	public string Desc {
		get {
			return field ?? throw new InvalidOperationException();
		}
		set;
	}

	/// <summary>
	/// 名前
	/// </summary>
	public string Name {
		get {
			return field ?? throw new InvalidOperationException();
		}
		set;
	}

	/// <summary>
	/// 位置情報
	/// </summary>
	public Position Position {
		get {
			return field ?? throw new InvalidOperationException();
		}
		set;
	}
}