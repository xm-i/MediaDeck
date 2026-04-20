using MediaDeck.Common.Base;
using MediaDeck.Core.Models.Repositories;

namespace MediaDeck.ViewModels.Panes.RepositoryPanes;

public class RepositoryViewModelBase(string name, RepositoryBase model) : ViewModelBase {
	public string Name {
		get;
	} = name;

	public RepositoryBase Model {
		get;
	} = model;
}