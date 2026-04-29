using MediaDeck.Composition.Enum;
using MediaDeck.MediaItemTypes.Base.Models;

namespace MediaDeck.MediaItemTypes.Unknown.Models;

[Inject(InjectServiceLifetime.Transient)]
public class UnknownMediaItemModel(UnknownMediaItemOperator fileOperator, UnknownMediaItemTypeProvider mediaItemTypeProvider, IServiceProvider scopedServiceProvider) : BaseMediaItemModel(fileOperator, MediaType.Unknown, mediaItemTypeProvider, scopedServiceProvider);