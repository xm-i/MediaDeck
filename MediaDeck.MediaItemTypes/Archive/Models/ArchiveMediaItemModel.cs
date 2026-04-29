using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.MediaItemTypes;
using MediaDeck.MediaItemTypes.Base.Models;

namespace MediaDeck.MediaItemTypes.Archive.Models;

[Inject(InjectServiceLifetime.Transient)]
public class ArchiveMediaItemModel(ArchiveMediaItemOperator fileOperator, IMediaItemTypeProvider mediaItemTypeProvider, IServiceProvider scopedServiceProvider) : BaseMediaItemModel(fileOperator, MediaType.Archive, mediaItemTypeProvider, scopedServiceProvider);