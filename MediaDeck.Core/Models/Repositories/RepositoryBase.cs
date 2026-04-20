using MediaDeck.Common.Base;

namespace MediaDeck.Core.Models.Repositories;

public abstract class RepositoryBase : ModelBase {
	public abstract Task Load();
}