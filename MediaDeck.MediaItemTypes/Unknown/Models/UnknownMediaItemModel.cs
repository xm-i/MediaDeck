using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.MediaItemTypes;
using MediaDeck.MediaItemTypes.Base.Models;

namespace MediaDeck.MediaItemTypes.Unknown.Models;

public class UnknownMediaItemModel(long id, string filePath, UnknownMediaItemOperator fileOperator, IMediaItemTypeProvider mediaItemTypeProvider, IServiceProvider scopedServiceProvider) : BaseMediaItemModel(id, filePath, fileOperator, MediaType.Unknown, mediaItemTypeProvider, scopedServiceProvider);