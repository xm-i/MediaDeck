using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.MediaItemTypes;
using MediaDeck.Composition.Stores.Config.Model;
using MediaDeck.Database.Tables;
using MediaDeck.MediaItemTypes.UI.Base;

namespace MediaDeck.MediaItemTypes.UI.Video;

[Inject(InjectServiceLifetime.Singleton, typeof(IMediaItemTypeProvider))]
public class VideoMediaItemTypeProvider : BaseMediaItemTypeProvider {

	public VideoMediaItemTypeProvider(ConfigModel config) : base(config, MediaType.Video) {
	}

	public override IQueryable<MediaItem> IncludeTables(IQueryable<MediaItem> MediaItems) {
		return MediaItems
			.Include(mf => mf.VideoFile);
	}
}