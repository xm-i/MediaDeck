namespace MediaDeck.Utils.Tools;

public static class R3Utility
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
}
