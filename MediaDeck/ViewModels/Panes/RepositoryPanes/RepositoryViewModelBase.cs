using MediaDeck.Models.Repositories;

namespace MediaDeck.ViewModels.Panes.RepositoryPanes;

public class RepositoryViewModelBase(string name, RepositoryBase model) {
	public string Name {
		get;
	} = name;

	public RepositoryBase Model {
		get;
	} = model;
}
