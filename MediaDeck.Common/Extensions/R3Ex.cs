using System.Collections;
using System.ComponentModel;

namespace MediaDeck.Common.Extensions;

public static class R3Ex {
	public static Observable<Unit> ToUnit<T>(this Observable<T> observable) {
		return observable.Select(_ => Unit.Default);
	}

	/// <summary>
	/// ReactiveProperty を 双方向バインド可能な BindableReactiveProperty に変換し、指定された CompositeDisposable に登録します。
	/// </summary>
	/// <typeparam name="T">値の型</typeparam>
	/// <param name="source">変換元の ReactiveProperty</param>
	/// <param name="initialValue">初期値</param>
	/// <param name="disposables">破棄を管理する CompositeDisposable</param>
	/// <returns>双方向バインドされた BindableReactiveProperty</returns>
	public static BindableReactiveProperty<T> ToTwoWayBindableReactiveProperty<T>(this ReactiveProperty<T> source, T initialValue = default!, CompositeDisposable? disposables = null) {
		var bindable = source.ToBindableReactiveProperty(initialValue);
		var d1 = bindable.Subscribe(x => {
			if (!SequenceEqualAwareEquals(source.Value, x)) {
				source.Value = x;
			}
		});
		if (disposables != null) {
			bindable.AddTo(disposables);
			d1.AddTo(disposables);
		}
		return bindable;
	}

	/// <summary>
	/// 相互変換関数を用いて、ReactiveProperty を双方向バインド可能な BindableReactiveProperty に変換し、指定された CompositeDisposable に登録します。
	/// </summary>
	/// <typeparam name="TProperty">変換元の型</typeparam>
	/// <typeparam name="TResult">変換先の型</typeparam>
	/// <param name="source">変換元の ReactiveProperty</param>
	/// <param name="convert">変換関数</param>
	/// <param name="convertBack">逆変換関数</param>
	/// <param name="initialValue">初期値</param>
	/// <param name="disposables">破棄を管理する CompositeDisposable</param>
	/// <returns>双方向バインドされた BindableReactiveProperty</returns>
	public static BindableReactiveProperty<TResult> ToTwoWayBindableReactiveProperty<TProperty, TResult>(this ReactiveProperty<TProperty> source, Func<TProperty, TResult> convert, Func<TResult, TProperty> convertBack, TResult initialValue = default!, CompositeDisposable? disposables = null) {
		var resultRp = new BindableReactiveProperty<TResult>(initialValue);
		var d1 = source.Subscribe(x => {
			var converted = convert(x);
			if (!SequenceEqualAwareEquals(resultRp.Value, converted)) {
				resultRp.Value = converted;
			}
		});
		var d2 = resultRp.Subscribe(x => {
			var back = convertBack(x);
			if (!SequenceEqualAwareEquals(source.Value, back)) {
				source.Value = back;
			}
		});
		if (disposables != null) {
			resultRp.AddTo(disposables);
			d1.AddTo(disposables);
			d2.AddTo(disposables);
		}
		return resultRp;
	}

	/// <summary>
	/// 相互変換関数を用いて、ReactiveProperty を別の型の双方向 ReactiveProperty に変換し、指定された CompositeDisposable に登録します。
	/// </summary>
	/// <typeparam name="TProperty">変換元の型</typeparam>
	/// <typeparam name="TResult">変換先の型</typeparam>
	/// <param name="source">変換元の ReactiveProperty</param>
	/// <param name="convert">変換関数</param>
	/// <param name="convertBack">逆変換関数</param>
	/// <param name="initialValue">初期値</param>
	/// <param name="disposables">破棄を管理する CompositeDisposable</param>
	/// <returns>双方向連携された ReactiveProperty</returns>
	public static ReactiveProperty<TResult> ToTwoWayReactiveProperty<TProperty, TResult>(this ReactiveProperty<TProperty> source, Func<TProperty, TResult> convert, Func<TResult, TProperty> convertBack, TResult initialValue = default!, CompositeDisposable? disposables = null) {
		var resultRp = new ReactiveProperty<TResult>(initialValue);
		var d1 = source.Subscribe(x => {
			var converted = convert(x);
			if (!SequenceEqualAwareEquals(resultRp.Value, converted)) {
				resultRp.Value = converted;
			}
		});
		var d2 = resultRp.Subscribe(x => {
			var back = convertBack(x);
			if (!SequenceEqualAwareEquals(source.Value, back)) {
				source.Value = back;
			}
		});
		if (disposables != null) {
			resultRp.AddTo(disposables);
			d1.AddTo(disposables);
			d2.AddTo(disposables);
		}
		return resultRp;
	}

	public static Observable<DataErrorsChangedEventArgs> ErrorsChangedAsObservable<T>(this BindableReactiveProperty<T> rp) {
		return Observable.FromEvent<EventHandler<DataErrorsChangedEventArgs>, DataErrorsChangedEventArgs>(h => (sender, e) => h(e),
			h => rp.ErrorsChanged += h,
			h => rp.ErrorsChanged -= h);
	}

	private static bool SequenceEqualAwareEquals<T>(T? left, T? right) {
		if (ReferenceEquals(left, right))
			return true;
		if (left is null || right is null)
			return false;
		if (left is string s1 && right is string s2)
			return s1 == s2;

		if (left is IEnumerable leftEnumerable && right is IEnumerable rightEnumerable) {
			return leftEnumerable.Cast<object>().SequenceEqual(rightEnumerable.Cast<object>());
		}

		return EqualityComparer<T>.Default.Equals(left, right);
	}
}