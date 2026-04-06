using MediaDeck.Composition.Stores.Config.Model;

namespace MediaDeck.Core.Stores.Config;

public interface IConfigStore {
	public IServiceProvider ScopedService {
		get;
	}

	public ConfigModel Config {
		get;
	}

	public void Load();

	public void Save();
}