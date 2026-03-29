namespace MediaDeck.Core.Primitives;

public class DisplayObject<T>(string displayString, T value) {
	public string DisplayString {
		get;
		init;
	} = displayString;

	public T Value {
		get;
	} = value;
}