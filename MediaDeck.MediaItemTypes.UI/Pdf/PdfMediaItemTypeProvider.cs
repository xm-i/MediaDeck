using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.MediaItemTypes;
using MediaDeck.Composition.Stores.Config.Model;
using MediaDeck.Database.Tables;
using MediaDeck.MediaItemTypes.UI.Base;

namespace MediaDeck.MediaItemTypes.UI.Pdf;

[Inject(InjectServiceLifetime.Singleton, typeof(IMediaItemTypeProvider))]
public class PdfMediaItemTypeProvider : BaseMediaItemTypeProvider {

	public PdfMediaItemTypeProvider(ConfigModel config) : base(config, MediaType.Pdf) {
	}


	public override IQueryable<MediaItem> IncludeTables(IQueryable<MediaItem> MediaItems) {
		return MediaItems
			.Include(mf => mf.Container);
	}
}