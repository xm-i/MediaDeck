using MediaDeck.Composition.Enum;
using MediaDeck.MediaItemTypes.Base.Models;

namespace MediaDeck.MediaItemTypes.Archive.Models;

[Inject(InjectServiceLifetime.Transient)]
public class ArchiveMediaItemModel(ArchiveMediaItemOperator fileOperator, ArchiveMediaItemTypeProvider mediaItemTypeProvider, IServiceProvider scopedServiceProvider) : BaseMediaItemModel(fileOperator, MediaType.Archive, mediaItemTypeProvider, scopedServiceProvider) {
	/// <summary>
	/// アーカイブ内ファイル数
	/// </summary>
	public int? FileCount {
		get;
		set;
	}
}