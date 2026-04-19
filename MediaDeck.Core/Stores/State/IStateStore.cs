using MediaDeck.Composition.Stores.State.Model;

namespace MediaDeck.Core.Stores.State;

public interface IStateStore {
	public IServiceProvider ScopedService {
		get;
	}

	public AppStateModel AppState {
		get;
	}

	public RootStateModel RootState {
		get;
	}

	/// <summary>
	/// 指定したTabStateModelをルート状態に追加する
	/// </summary>
	public void RegisterTab(TabStateModel tabState);

	/// <summary>
	/// 指定したTabStateModelをルート状態から除去する
	/// </summary>
	public void UnregisterTab(TabStateModel tabState);

	public void Load();

	public void Save();
}