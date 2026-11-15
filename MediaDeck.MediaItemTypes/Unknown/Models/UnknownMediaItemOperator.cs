using System.Threading.Tasks;
using MediaDeck.Composition.Database;
using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Tables;
using MediaDeck.MediaItemTypes.Base.Models;

namespace MediaDeck.MediaItemTypes.Unknown.Models;

/// <summary>
/// 未知メディアアイテムの操作クラス
/// </summary>
[Inject(InjectServiceLifetime.Transient)]
public partial class UnknownMediaItemOperator : BaseMediaItemOperator {
	/// <summary>
	/// コンストラクタ
	/// </summary>
	/// <param name="dbFactory">DbContextファクトリ</param>
	/// <param name="updateFileHashBackgroundService">ハッシュ更新サービス</param>
	/// <param name="databaseWriteCoordinator">データベース書き込みコーディネーター</param>
	public UnknownMediaItemOperator(
		IDbContextFactory<MediaDeckDbContext> dbFactory,
		IFileHashUpdatorService updateFileHashBackgroundService,
		IDatabaseWriteCoordinator databaseWriteCoordinator)
		: base(dbFactory, updateFileHashBackgroundService, databaseWriteCoordinator, MediaType.Unknown) { }

	public override Task<MediaItem?> RegisterMediaItemAsync(string filePath) {
		throw new NotSupportedException("Unknown file type cannot be registered.");
	}
}