using System.Threading.Tasks;

using MediaDeck.Composition.Enum;
using MediaDeck.Database;
using MediaDeck.Database.Tables;
using MediaDeck.MediaItemTypes.Base.Models;

namespace MediaDeck.MediaItemTypes.Unknown.Models;

[Inject(InjectServiceLifetime.Transient)]
public partial class UnknownMediaItemOperator : BaseMediaItemOperator {
	public UnknownMediaItemOperator(
		IDbContextFactory<MediaDeckDbContext> dbFactory,
		IFileHashUpdatorService updateFileHashBackgroundService)
		: base(dbFactory, updateFileHashBackgroundService, MediaType.Unknown) { }

	public override Task<MediaItem?> RegisterMediaItemAsync(string filePath) {
		throw new NotSupportedException("Unknown file type cannot be registered.");
	}
}