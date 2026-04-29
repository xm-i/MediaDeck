using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.MediaItemTypes;
using MediaDeck.Composition.Stores.Config.Model;
using MediaDeck.Database.Tables;
using MediaDeck.MediaItemTypes.Base;

namespace MediaDeck.MediaItemTypes.Unknown;

[Inject(InjectServiceLifetime.Singleton)]
[Inject(InjectServiceLifetime.Singleton, typeof(IMediaItemTypeProvider))]
public class UnknownMediaItemTypeProvider : BaseMediaItemTypeProvider {

	public UnknownMediaItemTypeProvider(ConfigModel config) : base(config, MediaType.Unknown) {
	}

	public override bool IsTargetPath(string path) {
		return false;
	}

	public override IQueryable<MediaItem> IncludeTables(IQueryable<MediaItem> MediaItems) {
		return MediaItems;
	}
}