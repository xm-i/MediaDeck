using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

using MediaDeck.Composition.Interfaces;

namespace MediaDeck.Common.Base;

/// <summary>
/// Model基底クラス
/// </summary>
public class ModelBase : DisposableBase, IModelBase {
	/// <summary>
	/// バッキングフィールド
	/// </summary>
	private readonly ConcurrentDictionary<string, object?> _backingFields = new();

	/// <summary>
	/// バッキングフィールドから値を取得(Boxingが発生するのでパフォーマンスが重要な場面では使わない)
	/// </summary>
	/// <typeparam name="T">型</typeparam>
	/// <param name="member">メンバー名</param>
	/// <returns>値</returns>
	protected T? GetValue<T>([CallerMemberName] string member = "") {
		return this.GetValue<T?>(() => default, member);
	}

	/// <summary>
	/// バッキングフィールドから値を取得(Boxingが発生するのでパフォーマンスが重要な場面では使わない)
	/// </summary>
	/// <typeparam name="T">型</typeparam>
	/// <param name="valueFactory">バッキングフィールドに値がなかった場合の値生成関数</param>
	/// <param name="member">メンバー名</param>
	/// <returns>値</returns>
	protected T? GetValue<T>(Func<T?> valueFactory, [CallerMemberName] string member = "") {
		return
			(T?)this
				._backingFields
				.GetOrAdd(member, _ => valueFactory());
	}

	/// <summary>
	/// バッキングフィールドに値を設定(Boxingが発生するのでパフォーマンスが重要な場面では使わない)
	/// </summary>
	/// <typeparam name="T">型</typeparam>
	/// <param name="value">値</param>
	/// <param name="member">メンバー名</param>
	protected void SetValue<T>(T value, [CallerMemberName] string member = "") {
		if (EqualityComparer<T>.Default.Equals(this.GetValue<T>(member), value)) {
			return;
		}
		this._backingFields[member] = value;
		this.OnPropertyChanged(member);
	}
}