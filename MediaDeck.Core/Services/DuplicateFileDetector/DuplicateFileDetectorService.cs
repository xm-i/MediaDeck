using MediaDeck.Common.Base;
using MediaDeck.Database;

namespace MediaDeck.Core.Services.DuplicateFileDetector;

/// <summary>
/// 重複ファイル検出モデル
/// </summary>
[Inject(InjectServiceLifetime.Transient)]
public class DuplicateFileDetectorService : ModelBase {
	private readonly IDbContextFactory<MediaDeckDbContext> _dbFactory;

	/// <summary>
	/// 重複ファイルグループリスト
	/// </summary>
	public ObservableList<DuplicateFileGroup> DuplicateGroups {
		get;
	} = [];

	/// <summary>
	/// 検出中フラグ
	/// </summary>
	public ReactiveProperty<bool> IsDetecting {
		get;
	} = new(false);

	/// <summary>
	/// 検出完了フラグ
	/// </summary>
	public ReactiveProperty<bool> IsCompleted {
		get;
	} = new(false);

	/// <summary>
	/// 重複グループ数
	/// </summary>
	public ReactiveProperty<int> DuplicateGroupCount {
		get;
	} = new(0);

	/// <summary>
	/// 重複ファイル総数
	/// </summary>
	public ReactiveProperty<int> DuplicateFileCount {
		get;
	} = new(0);

	public DuplicateFileDetectorService(IDbContextFactory<MediaDeckDbContext> dbFactory) {
		this._dbFactory = dbFactory;
	}

	/// <summary>
	/// 重複ファイルを検出する
	/// </summary>
	/// <param name="useFullHash">完全ハッシュを使用するか（falseの場合はPreHashを使用）</param>
	public async Task DetectDuplicatesAsync(bool useFullHash) {
		this.IsDetecting.Value = true;
		this.IsCompleted.Value = false;
		this.DuplicateGroups.Clear();

		try {
			List<DuplicateFileGroup> groups;

			if (useFullHash) {
				await using var db = await this._dbFactory.CreateDbContextAsync();
				groups = await db.MediaFiles
					.Include(x => x.MediaFileTags)
					.ThenInclude(x => x.Tag)
					.ThenInclude(x => x.TagCategory)
					.Include(x => x.MediaFileTags)
					.ThenInclude(x => x.Tag)
					.ThenInclude(x => x.TagAliases)
					.Where(x => x.FullHash != null)
					.GroupBy(x => x.FullHash!)
					.Where(g => g.Count() > 1)
					.Select(g => new DuplicateFileGroup { Hash = g.Key, Files = g.OrderBy(f => f.FilePath).ToList() })
					.ToListAsync();
			} else {
				await using var db = await this._dbFactory.CreateDbContextAsync();
				groups = await db.MediaFiles
					.Include(x => x.MediaFileTags)
					.ThenInclude(x => x.Tag)
					.ThenInclude(x => x.TagCategory)
					.Include(x => x.MediaFileTags)
					.ThenInclude(x => x.Tag)
					.ThenInclude(x => x.TagAliases)
					.Where(x => x.PreHash != null)
					.GroupBy(x => x.PreHash!)
					.Where(g => g.Count() > 1)
					.Select(g => new DuplicateFileGroup { Hash = g.Key, Files = g.OrderBy(f => f.FilePath).ToList() })
					.ToListAsync();
			}

			foreach (var group in groups.OrderBy(x => x.RepresentativeFileName)) {
				this.DuplicateGroups.Add(group);
			}

			this.DuplicateGroupCount.Value = this.DuplicateGroups.Count;
			this.DuplicateFileCount.Value = this.DuplicateGroups.Sum(g => g.Files.Count);
		} finally {
			this.IsDetecting.Value = false;
			this.IsCompleted.Value = true;
		}
	}
}