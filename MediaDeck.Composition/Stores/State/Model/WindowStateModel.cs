using R3.JsonConfig.Attributes;

namespace MediaDeck.Composition.Stores.State.Model;

/// <summary>
/// ウィンドウごとの状態を保持するモデル。
/// 各ウィンドウが独自のタブリストを持つ。
/// </summary>
[Inject(InjectServiceLifetime.Transient)]
[GenerateR3JsonConfigDto]
public class WindowStateModel {
	/// <summary>
	/// ウィンドウの一意識別子
	/// </summary>
	public Guid WindowId {
		get;
		set;
	} = Guid.NewGuid();

	/// <summary>
	/// このウィンドウが持つタブのIDリスト
	/// </summary>
	public ObservableList<Guid> TabIds {
		get;
	} = [];

	/// <summary>
	/// アクティブだったタブのID
	/// </summary>
	public ReactiveProperty<Guid?> SelectedTabId {
		get;
		set;
	} = new();

}