using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.MediaItemTypes;
using MediaDeck.Composition.Stores.Config.Model;
using MediaDeck.Composition.Tables;
using MediaDeck.MediaItemTypes.Base;

namespace MediaDeck.MediaItemTypes.Archive;

[Inject(InjectServiceLifetime.Singleton)]
[Inject(InjectServiceLifetime.Singleton, typeof(IMediaItemTypeProvider))]
public class ArchiveMediaItemTypeProvider : BaseMediaItemTypeProvider {

	public ArchiveMediaItemTypeProvider(ConfigModel config) : base(config, MediaType.Archive) {
	}

	public override IQueryable<MediaItem> IncludeTables(IQueryable<MediaItem> MediaItems) {
		return MediaItems
			.Include(mf => mf.Container);
	}
}