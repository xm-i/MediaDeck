namespace MediaDeck.Utils.Objects;

public class DisplayObject<T>(string displayString, T value) {
	public string DisplayString {
		get;
		init;
	} = displayString;

	public T Value {
		get;
	} = value;
}
