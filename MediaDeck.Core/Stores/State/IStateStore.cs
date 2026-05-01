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

	public void Load();

	public void Save();
}