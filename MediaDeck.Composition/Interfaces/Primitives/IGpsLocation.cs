namespace MediaDeck.Composition.Interfaces.Primitives; 
public interface IGpsLocation: IComparable<IGpsLocation>, IComparable {
	/// <summary>
	/// 緯度
	/// </summary>
	public double Latitude {
		get;
	}

	/// <summary>
	/// 経度
	/// </summary>
	public double Longitude {
		get;
	}

	/// <summary>
	/// 高度
	/// </summary>
	public double? Altitude {
		get;
	}
}
