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

	/// <summary>
	/// サムネイル上オーバーレイ表示
	/// </summary>
	public ReactiveProperty<bool> ShowOverlay {
		get;
	} = new(true);

	/// <summary>
	/// 情報エリア表示
	/// </summary>
	public ReactiveProperty<bool> ShowInfo {
		get;
	} = new(true);
}