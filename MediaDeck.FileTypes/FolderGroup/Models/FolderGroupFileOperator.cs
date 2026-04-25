using System.Threading.Tasks;

using MediaDeck.Composition.Enum;
using MediaDeck.Database;
using MediaDeck.Database.Tables;
using MediaDeck.FileTypes.Base.Models;

namespace MediaDeck.FileTypes.FolderGroup.Models;

[Inject(InjectServiceLifetime.Transient)]
internal partial class FolderGroupFileOperator : BaseFileOperator {
	public FolderGroupFileOperator(
		IDbContextFactory<MediaDeckDbContext> dbFactory,
		IFileHashUpdatorService updateFileHashBackgroundService)
		: base(dbFactory, updateFileHashBackgroundService, MediaType.FolderGroup) { }

	public override Task<MediaFile?> RegisterFileAsync(string filePath) {
		throw new NotSupportedException("FolderGroup type cannot be registered from file path.");
	}
}
