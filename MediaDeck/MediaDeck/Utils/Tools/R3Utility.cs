namespace MediaDeck.Utils.Tools;

public static class R3Utility
{
	public static Observable<Unit> ToUnit<T>(this Observable<T> observable) {
		return observable.Select(_ => Unit.Default);
	}
}
