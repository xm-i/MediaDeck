using MediaDeck.Composition.Interfaces.Files;
using MediaDeck.Composition.Objects;

namespace MediaDeck.Models.Files.Filter.FilterItemObjects;
/// <summary>
/// 座標に関するフィルターアイテムオブジェクト
/// </summary>
/// <remarks>
/// どのプロパティに値を代入するかで複数の役割を持つ
/// ・地名フィルター
/// ・座標情報を含むか否かのフィルター
/// ・座標範囲フィルター
/// </remarks>
public class LocationFilterItemObject : IFilterItemObject {
	/// <summary>
	/// 表示名
	/// </summary>
	public string DisplayName {
		get {
			if (this.Text != null) {
				return $"Includes {this.Text} in place name";
			}
			if (this.Contains is { } b) {
				return b ? "Contains coordinate information" : "Does not contain coordinate information";
			}
			if (this.LeftTop != null && this.RightBottom != null) {
				return $"Within the range of [{this.LeftTop}] and [{this.RightBottom}]";
			}

			return "Error";
		}
	}

	/// <summary>
	/// 地名に含まれる文字列
	/// </summary>
	public string? Text {
		get;
		set;
	}

	/// <summary>
	/// 座標情報を含む/含まない
	/// </summary>
	public bool? Contains {
		get;
		set;
	}

	/// <summary>
	/// 左上座標
	/// </summary>
	public GpsLocation? LeftTop {
		get;
		set;
	}

	/// <summary>
	/// 右上座標
	/// </summary>
	public GpsLocation? RightBottom {
		get;
		set;
	}

	[Obsolete("for serialize")]
	public LocationFilterItemObject() {
	}

	/// <summary>
	/// コンストラクタ
	/// </summary>
	/// <param name="text">地名に含まれる文字列</param>
	public LocationFilterItemObject(string text) {
		this.Text = text;
	}

	/// <summary>
	/// コンストラクタ
	/// </summary>
	/// <param name="contains">地名に含まれる文字列</param>
	public LocationFilterItemObject(bool contains) {
		this.Contains = contains;
	}

	/// <summary>
	/// コンストラクタ
	/// </summary>
	/// <param name="leftTop">左上座標</param>
	/// <param name="rightBottom">右下座標</param>
	public LocationFilterItemObject(GpsLocation leftTop, GpsLocation rightBottom) {
		this.LeftTop = leftTop;
		this.RightBottom = rightBottom;
	}
}
