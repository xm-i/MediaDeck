using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.MediaItemTypes;
using MediaDeck.MediaItemTypes.Base.Models;

namespace MediaDeck.MediaItemTypes.Archive.Models;

public class ArchiveMediaItemModel(long id, string filePath, ArchiveMediaItemOperator fileOperator, IMediaItemType mediaItemType, IServiceProvider scopedServiceProvider) : BaseMediaItemModel(id, filePath, fileOperator, MediaType.Archive, mediaItemType, scopedServiceProvider);