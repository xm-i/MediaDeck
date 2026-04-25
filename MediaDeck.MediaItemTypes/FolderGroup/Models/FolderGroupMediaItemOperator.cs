using System.Threading.Tasks;

using MediaDeck.Composition.Enum;
using MediaDeck.Database;
using MediaDeck.Database.Tables;
using MediaDeck.MediaItemTypes.Base.Models;

namespace MediaDeck.MediaItemTypes.FolderGroup.Models;

[Inject(InjectServiceLifetime.Transient)]
internal partial class FolderGroupMediaItemOperator : BaseMediaItemOperator {
	public FolderGroupMediaItemOperator(
		IDbContextFactory<MediaDeckDbContext> dbFactory,
		IFileHashUpdatorService updateFileHashBackgroundService)
		: base(dbFactory, updateFileHashBackgroundService, MediaType.FolderGroup) { }

	public override async Task<MediaItem?> RegisterMediaItemAsync(string filePath) {
		throw new NotSupportedException("FolderGroup type cannot be registered from file path.");
	}
}