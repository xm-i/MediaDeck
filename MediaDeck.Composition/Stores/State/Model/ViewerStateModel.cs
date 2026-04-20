using R3.JsonConfig.Attributes;

namespace MediaDeck.Composition.Stores.State.Model;

[Inject(InjectServiceLifetime.Transient)]
[GenerateR3JsonConfigDto]
public class ViewerStateModel {
	/// <summary>
	/// アイテムサイズ (Zoom)
	/// </summary>
	public ReactiveProperty<int> ItemSize {
		get;
	} = new(150);
}