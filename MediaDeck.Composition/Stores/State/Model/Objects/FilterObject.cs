using MediaDeck.Composition.Interfaces.Files;

using R3.JsonConfig.Attributes;

namespace MediaDeck.Composition.Stores.State.Model.Objects;
/// <summary>
/// フィルター設定復元用オブジェクト
/// </summary>
[Inject(InjectServiceLifetime.Scoped)]
[GenerateR3JsonConfigDefaultDto]
public class FilterObject {
	/// <summary>
	/// ID
	/// </summary>
	public Guid Id {
		get;
		set;
	} = Guid.NewGuid();

	/// <summary>
	/// 表示名
	/// </summary>
	public ReactiveProperty<string> DisplayName {
		get;
	} = new();

	/// <summary>
	/// フィルター条件オブジェクト
	/// </summary>
	public ObservableList<IFilterItemObject> FilterItemObjects {
		get;
	} = [];
}
