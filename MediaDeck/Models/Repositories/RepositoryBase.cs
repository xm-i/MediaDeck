using System.Threading.Tasks;

namespace MediaDeck.Models.Repositories;

public abstract class RepositoryBase {
	public abstract Task Load();
}
