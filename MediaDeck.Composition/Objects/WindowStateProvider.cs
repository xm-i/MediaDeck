using MediaDeck.Composition.Stores.State.Model;

namespace MediaDeck.Composition.Objects;

/// <summary>
/// ウィンドウのスコープ内で自身のWindowStateModelを保持・提供するクラス。
/// </summary>
[Inject(InjectServiceLifetime.Scoped)]
public class WindowStateProvider {
	public WindowStateModel? State {
		get;
		set;
	}
}