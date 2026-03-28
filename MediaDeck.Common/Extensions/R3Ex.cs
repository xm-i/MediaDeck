using System.ComponentModel;

namespace MediaDeck.Common.Extensions;

public static class R3Ex
{
	public static Observable<Unit> ToUnit<T>(this Observable<T> observable) {
		return observable.Select(_ => Unit.Default);
	}
	public static BindableReactiveProperty<T> ToTwoWayBindableReactiveProperty<T>(this ReactiveProperty<T> source, T initialValue = default!) {
		var bindable = source.ToBindableReactiveProperty(initialValue);
		bindable.Subscribe(x => {
			source.Value = x;
		});
		return bindable;
	}
	public static ReactiveProperty<TResult> ToTwoWayReactiveProperty<TProperty, TResult>(this ReactiveProperty<TProperty> source, Func<TProperty, TResult> convert, Func<TResult, TProperty> convertBack, TResult initialValue = default!) {
		var resultRp = new ReactiveProperty<TResult>(initialValue);
		source.Subscribe(x => {
			resultRp.Value = convert(x);
		});
		resultRp.Subscribe(x => {
			source.Value = convertBack(x);
		});
		return resultRp;
	}

	public static Observable<DataErrorsChangedEventArgs> ErrorsChangedAsObservable<T>(this BindableReactiveProperty<T> rp) {
		return Observable.FromEvent<EventHandler<DataErrorsChangedEventArgs>, DataErrorsChangedEventArgs>(
			h => (sender, e) => h(e),
			h => rp.ErrorsChanged += h,
			h => rp.ErrorsChanged -= h);
	}
}
