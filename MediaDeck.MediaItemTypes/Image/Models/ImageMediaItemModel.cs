using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Stores.Config.Model;
using MediaDeck.MediaItemTypes.Base.Models;

namespace MediaDeck.MediaItemTypes.Image.Models;

internal class ImageMediaItemModel(long id, string filePath, ImageMediaItemOperator fileOperator, ConfigModel config) : BaseMediaItemModel(id, filePath, fileOperator, MediaType.Image, config);