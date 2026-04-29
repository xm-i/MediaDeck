using System.Collections;
using System.ComponentModel;

namespace MediaDeck.Common.Extensions;

public static class R3Ex {
	public static Observable<Unit> ToUnit<T>(this Observable<T> observable) {
		return observable.Select(_ => Unit.Default);
	}

	public static BindableReactiveProperty<T> ToTwoWayBindableReactiveProperty<T>(this ReactiveProperty<T> source, T initialValue = default!, CompositeDisposable? disposables = null) {
		var bindable = source.ToBindableReactiveProperty(initialValue);
		var d1 = bindable.Subscribe(x => {
			if (!SequenceEqualAwareEquals(source.Value, x)) {
				source.Value = x;
			}
		});
		if (disposables != null) {
			d1.AddTo(disposables);
		}
		return bindable;
	}

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
			d1.AddTo(disposables);
			d2.AddTo(disposables);
		}
		return resultRp;
	}
	public static ReactiveProperty<TResult> ToTwoWayReactiveProperty<TProperty, TResult>(this ReactiveProperty<TProperty> source, Func<TProperty, TResult> convert, Func<TResult, TProperty> convertBack, TResult initialValue = default!, CompositeDisposable? disposables = null) {
		var resultRp = new ReactiveProperty<TResult>(initialValue);
		var d1 = source.Pairwise().Where(x => !SequenceEqualAwareEquals(x.Previous, x.Current)).Select(x => x.Current).Subscribe(x => {
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