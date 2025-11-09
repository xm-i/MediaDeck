using MediaDeck.Models.Repositories;

namespace MediaDeck.ViewModels.Panes.RepositoryPanes;

public class RepositoryViewModelBase(RepositoryBase model) {
	public RepositoryBase Model {
		get;
	} = model;
}
