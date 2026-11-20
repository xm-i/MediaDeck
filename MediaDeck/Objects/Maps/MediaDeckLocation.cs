using MediaDeck.Composition.Interfaces.Primitives;

namespace MediaDeck.Objects.Maps;

/// <summary>
/// MediaDeck用座標クラス
/// </summary>
/// <remarks>
/// MapControl.Locationを継承し、UIコントロールで直接利用可能にする。
/// </remarks>
public class MediaDeckLocation : global::MapControl.Location, ILocation {
	/// <summary>
	/// コンストラクタ
	/// </summary>
	public MediaDeckLocation(double latitude, double longitude, double? altitude = null)
		: base(latitude, longitude) {
		this.Altitude = altitude;
	}

	/// <inheritdoc/>
	public double? Altitude {
		get;
	}

	/// <inheritdoc/>
	public int CompareTo(IGpsLocation? other) {
		if (other is null) {
			return 1;
		}

		var result = this.Latitude.CompareTo(other.Latitude);
		if (result != 0) {
			return result;
		}

		result = this.Longitude.CompareTo(other.Longitude);
		if (result != 0) {
			return result;
		}

		if (this.Altitude.HasValue && other.Altitude.HasValue) {
			return this.Altitude.Value.CompareTo(other.Altitude.Value);
		}

		if (this.Altitude.HasValue) {
			return 1;
		}

		if (other.Altitude.HasValue) {
			return -1;
		}

		return 0;
	}

	/// <inheritdoc/>
	public int CompareTo(object? obj) {
		if (obj is IGpsLocation other) {
			return this.CompareTo(other);
		}

		return 1;
	}

	public static bool operator ==(MediaDeckLocation? gl, MediaDeckLocation? gl2) {
		if (gl is null && gl2 is null) {
			return true;
		}
		if (gl is null || gl2 is null) {
			return false;
		}
		return gl.CompareTo(gl2) == 0;
	}

	public static bool operator !=(MediaDeckLocation? gl, MediaDeckLocation? gl2) {
		return !(gl == gl2);
	}

	public static bool operator <(MediaDeckLocation? gl, MediaDeckLocation? gl2) {
		if (gl == gl2) {
			return false;
		}
		if (gl == null) {
			return gl2!.CompareTo(gl) > 0;
		}
		return gl.CompareTo(gl2) < 0;
	}

	public static bool operator >(MediaDeckLocation? gl, MediaDeckLocation? gl2) {
		if (gl == gl2) {
			return false;
		}
		if (gl == null) {
			return gl2!.CompareTo(gl) < 0;
		}
		return gl.CompareTo(gl2) > 0;
	}

	public static bool operator <=(MediaDeckLocation? gl, MediaDeckLocation? gl2) {
		if (gl == gl2) {
			return true;
		}
		return gl < gl2;
	}

	public static bool operator >=(MediaDeckLocation? gl, MediaDeckLocation? gl2) {
		if (gl == gl2) {
			return true;
		}
		return gl > gl2;
	}
	public override string ToString() {
		return $"{this.Latitude} {this.Longitude} {this.Altitude}";
	}

	/// <inheritdoc/>
	public override bool Equals(object? obj) {
		if (obj is not IGpsLocation loc) {
			return false;
		}
		return base.Equals(obj) &&
			Nullable.Equals(this.Altitude, loc.Altitude);
	}

	public override int GetHashCode() {
		return this.Latitude.GetHashCode() ^ this.Longitude.GetHashCode() ^ this.Altitude.GetHashCode();
	}
}