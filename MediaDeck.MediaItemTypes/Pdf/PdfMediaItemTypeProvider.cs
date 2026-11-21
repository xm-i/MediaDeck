using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.MediaItemTypes;
using MediaDeck.Composition.Stores.Config.Model;
using MediaDeck.Composition.Tables;
using MediaDeck.MediaItemTypes.Base;

namespace MediaDeck.MediaItemTypes.Pdf;

[Inject(InjectServiceLifetime.Singleton)]
[Inject(InjectServiceLifetime.Singleton, typeof(IMediaItemTypeProvider))]
public class PdfMediaItemTypeProvider : BaseMediaItemTypeProvider {

	public PdfMediaItemTypeProvider(ConfigModel config) : base(config, MediaType.Pdf) {
	}


	/// <summary>
	/// クエリに必要な関連テーブルを含めます。
	/// </summary>
	/// <param name="MediaItems">対象クエリ</param>
	/// <returns>関連テーブルを含めたクエリ</returns>
	public override IQueryable<MediaItem> IncludeTables(IQueryable<MediaItem> MediaItems) {
		return MediaItems;
	}
}