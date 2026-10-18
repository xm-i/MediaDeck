using MediaDeck.Core.Models.Maps;
using Microsoft.UI;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

namespace MediaDeck.Views.Resources.Converters;

/// <summary>
/// マップピンの状態 (<see cref="PinState"/>) を描画用ブラシに変換するコンバーター。
/// </summary>
public class PinStateToBrushConverter : IValueConverter {
	private static readonly SolidColorBrush _defaultBrush = new(ColorHelper.FromArgb(255, 224, 224, 224));
	private static readonly SolidColorBrush _selectedBrush = new(Colors.DodgerBlue);
	private static readonly SolidColorBrush _indeterminateBrush = new(Colors.Orange);

	/// <summary>
	/// <see cref="PinState"/> の値に応じたブラシを返します。
	/// </summary>
	/// <param name="value">変換対象の <see cref="PinState"/></param>
	/// <param name="targetType">ターゲットの型</param>
	/// <param name="parameter">コンバーターパラメータ</param>
	/// <param name="language">言語情報</param>
	/// <returns>対応する <see cref="SolidColorBrush"/></returns>
	public object Convert(object value, Type targetType, object parameter, string language) {
		if (value is not PinState state) {
			return _defaultBrush;
		}

		return state switch {
			PinState.Selected => _selectedBrush,
			PinState.Indeterminate => _indeterminateBrush,
			_ => _defaultBrush
		};
	}

	/// <summary>
	/// 値の逆変換を行います（未サポート）。
	/// </summary>
	/// <param name="value">変換対象の値</param>
	/// <param name="targetType">ターゲットの型</param>
	/// <param name="parameter">コンバーターパラメータ</param>
	/// <param name="language">言語情報</param>
	/// <returns>常に例外をスローします</returns>
	public object ConvertBack(object value, Type targetType, object parameter, string language) {
		throw new NotImplementedException();
	}
}