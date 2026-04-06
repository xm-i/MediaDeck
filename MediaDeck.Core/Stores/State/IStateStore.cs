using MediaDeck.Composition.Stores.State.Model;

namespace MediaDeck.Core.Stores.State;

public interface IStateStore {
	public IServiceProvider ScopedService {
		get;
	}

	public StateModel State {
		get;
	}

	public void Load();

	public void Save();
}