using System.ComponentModel;

namespace MediaDeck.Common.Extensions;

public static class R3Ex {
	public static Observable<Unit> ToUnit<T>(this Observable<T> observable) {
		return observable.Select(_ => Unit.Default);
	}

	public static BindableReactiveProperty<T> ToTwoWayBindableReactiveProperty<T>(this ReactiveProperty<T> source, T initialValue = default!, CompositeDisposable? disposables = null) {
		var bindable = source.ToBindableReactiveProperty(initialValue);
		var d1 = bindable.Subscribe(x => {
			source.Value = x;
		});
		if (disposables != null) {
			d1.AddTo(disposables);
		}
		return bindable;
	}

	public static BindableReactiveProperty<TResult> ToTwoWayBindableReactiveProperty<TProperty, TResult>(this ReactiveProperty<TProperty> source, Func<TProperty, TResult> convert, Func<TResult, TProperty> convertBack, TResult initialValue = default!, CompositeDisposable? disposables = null) {
		var resultRp = new BindableReactiveProperty<TResult>(initialValue);
		var d1 = source.Subscribe(x => {
			resultRp.Value = convert(x);
		});
		var d2 = resultRp.Subscribe(x => {
			source.Value = convertBack(x);
		});
		if (disposables != null) {
			d1.AddTo(disposables);
			d2.AddTo(disposables);
		}
		return resultRp;
	}
	public static ReactiveProperty<TResult> ToTwoWayReactiveProperty<TProperty, TResult>(this ReactiveProperty<TProperty> source, Func<TProperty, TResult> convert, Func<TResult, TProperty> convertBack, TResult initialValue = default!, CompositeDisposable? disposables = null) {
		var resultRp = new ReactiveProperty<TResult>(initialValue);
		var d1 = source.Subscribe(x => {
			resultRp.Value = convert(x);
		});
		var d2 = resultRp.Subscribe(x => {
			source.Value = convertBack(x);
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
}