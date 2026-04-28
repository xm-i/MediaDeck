using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.MediaItemTypes;
using MediaDeck.Composition.Stores.Config.Model;
using MediaDeck.Database.Tables;
using MediaDeck.MediaItemTypes.UI.Base;

namespace MediaDeck.MediaItemTypes.UI.Archive;

[Inject(InjectServiceLifetime.Singleton, typeof(IMediaItemTypeProvider))]
public class ArchiveMediaItemTypeProvider : BaseMediaItemTypeProvider {

	public ArchiveMediaItemTypeProvider(ConfigModel config) : base(config, MediaType.Archive) {
	}

	public override IQueryable<MediaItem> IncludeTables(IQueryable<MediaItem> MediaItems) {
		return MediaItems
			.Include(mf => mf.Container);
	}
}