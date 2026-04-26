using System.IO;
using System.Threading.Tasks;

using MediaDeck.Composition.Interfaces.MediaItemTypes;
using MediaDeck.Composition.Interfaces.MediaItemTypes.Models;
using MediaDeck.Composition.Interfaces.MediaItemTypes.ViewModels;
using MediaDeck.Database;
using MediaDeck.Database.Tables;
using MediaDeck.MediaItemTypes.Base.Models;

using Microsoft.Extensions.Logging;

namespace MediaDeck.MediaItemTypes.FolderGroup.Models;

[Inject(InjectServiceLifetime.Transient)]
public class FolderGroupThumbnailPickerModel(
	IDbContextFactory<MediaDeckDbContext> dbFactory,
	ILogger<FolderGroupThumbnailPickerModel> logger,
	IFilePathService filePathService,
	IMediaItemTypeService mediaItemTypeService,
	IServiceProvider scopedServiceProvider)
	: BaseThumbnailPickerModel(dbFactory, logger, filePathService) {
	private readonly IDbContextFactory<MediaDeckDbContext> _dbFactory = dbFactory;
	private readonly IFilePathService _filePathService = filePathService;
	private readonly IMediaItemTypeService _mediaItemTypeService = mediaItemTypeService;
	private readonly IServiceProvider _scopedServiceProvider = scopedServiceProvider;

	/// <summary>
	/// フォルダ内のアイテムリスト
	/// </summary>
	public ObservableList<IMediaItemViewModel> Items { get; } = [];

	/// <summary>
	/// フォルダ内のアイテム一覧を読み込みます。
	/// </summary>
	/// <param name="folderPath">フォルダパス</param>
	/// <returns>アイテム一覧</returns>
	public async Task LoadItemsInFolderAsync(string folderPath) {
		await using var db = await this._dbFactory.CreateDbContextAsync();
		IQueryable<MediaItem> query = db
			.MediaItems
			.AsNoTracking();

		query = query
			.Include(mf => mf.MediaItemTags)
			.ThenInclude(mft => mft.Tag)
			.ThenInclude(t => t.TagCategory)
			.Include(mf => mf.MediaItemTags)
			.ThenInclude(mft => mft.Tag)
			.ThenInclude(t => t.TagAliases)
			.Include(mf => mf.Position);

		query = this._mediaItemTypeService.IncludeTables(query);

		var items = (await query.Where(x => x.DirectoryPath == folderPath)
			.OrderBy(x => x.FilePath)
			.ToListAsync())
			.Select(x => {
				var model = this._mediaItemTypeService.CreateMediaItemModelFromRecord(x, this._scopedServiceProvider);
				var vm = this._mediaItemTypeService.CreateMediaItemViewModel(model);
				return vm;
			});

		this.Items.Clear();
		this.Items.AddRange(items);
	}

	public byte[]? GetThumbnailBinary(IMediaItemModel model) {
		if (model.ThumbnailFilePath == null) {
			return null;
		}
		var path = this._filePathService.GetThumbnailAbsoluteFilePath(model.ThumbnailFilePath);
		return File.ReadAllBytes(path);
	}
}