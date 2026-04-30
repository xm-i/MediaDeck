using System.IO.Compression;

using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.MediaItemTypes;
using MediaDeck.Composition.Interfaces.MediaItemTypes.ViewModels;
using MediaDeck.MediaItemTypes.Archive.Models;
using MediaDeck.MediaItemTypes.Base.Models;
using MediaDeck.MediaItemTypes.Base.ViewModels;

namespace MediaDeck.MediaItemTypes.Archive.ViewModels;

/// <summary>
/// アーカイブ用の一括サムネイル再生成設定ViewModel。
/// 各アーカイブの "対象画像 N 番目" を代表画像として用いる。
/// </summary>
[Inject(InjectServiceLifetime.Transient)]
public class ArchiveBulkThumbnailConfigViewModel : BulkThumbnailConfigViewModelBase {
	private readonly ArchiveMediaItemOperator _archiveOperator;
	private readonly IMediaItemTypeService _mediaItemTypeService;

	public ArchiveBulkThumbnailConfigViewModel(ArchiveMediaItemOperator archiveOperator, IMediaItemTypeService mediaItemTypeService, BaseThumbnailPickerModel thumbnailPickerModel)
		: base(MediaType.Archive, thumbnailPickerModel) {
		this._archiveOperator = archiveOperator;
		this._mediaItemTypeService = mediaItemTypeService;
	}

	/// <summary>
	/// アーカイブ内の画像エントリのうち、何番目 (1始まり) を代表画像とするか。
	/// </summary>
	public BindableReactiveProperty<int> EntryIndex {
		get;
	} = new(1);

	protected override byte[]? GenerateThumbnail(IMediaItemViewModel target) {
		using var archive = ZipFile.OpenRead(target.FilePath);
		var images = archive.Entries.Where(x => this._mediaItemTypeService.IsTargetPath(x.Name, MediaType.Image)).ToList();
		if (images.Count == 0) {
			return null;
		}
		var index = Math.Clamp(this.EntryIndex.Value - 1, 0, images.Count - 1);
		var entry = images[index];
		return this._archiveOperator.CreateThumbnail(archive, 300, 300, entry.FullName);
	}
}