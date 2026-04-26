using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.MediaItemTypes.Models;
using MediaDeck.Composition.Interfaces.MediaItemTypes.ViewModels;
using MediaDeck.Composition.Interfaces.MediaItemTypes.Views;
using MediaDeck.Database.Tables;

namespace MediaDeck.Composition.Interfaces.MediaItemTypes;

/// <summary>
/// メディアアイテムタイプに関連する操作を提供するサービスインターフェース
/// </summary>
public interface IMediaItemTypeService {
	/// <summary>
	/// データベースレコードからメディアアイテムモデルを作成します。
	/// </summary>
	public IMediaItemModel CreateMediaItemModelFromRecord(MediaItem MediaItem, IServiceProvider scopedServiceProvider);

	/// <summary>
	/// メディアアイテムモデルからビューモデルを作成します。
	/// </summary>
	public IMediaItemViewModel CreateMediaItemViewModel(IMediaItemModel fileModel);

	/// <summary>
	/// ビューモデルに対応する詳細プレビューコントロールビューを作成します。
	/// </summary>
	public IDetailViewerPreviewControlView CreateDetailViewerPreviewControlView(IMediaItemViewModel fileViewModel);

	/// <summary>
	/// ビューモデルに対応するサムネイル選択ビューモデルを作成します。
	/// </summary>
	public IThumbnailPickerViewModel CreateThumbnailPickerViewModel(IMediaItemViewModel fileViewModel);

	/// <summary>
	/// ビューモデルに対応するサムネイル選択ビューを作成します。
	/// </summary>
	public IThumbnailPickerView CreateThumbnailPickerView(IMediaItemViewModel fileViewModel);

	/// <summary>
	/// 全てのメディアアイテムタイプに対するオペレーターを作成します。
	/// </summary>
	public IMediaItemOperator[] CreateMediaItemOperators();

	/// <summary>
	/// クエリにメディアアイテムタイプ固有の関連テーブルを含めます。
	/// </summary>
	public IQueryable<MediaItem> IncludeTables(IQueryable<MediaItem> MediaItems);

	/// <summary>
	/// パスに対応するメディアアイテムタイプを取得します。
	/// </summary>
	public IMediaItemType GetMediaItemType(string path);

	/// <summary>
	/// 指定したメディアタイプに対応するメディアアイテムタイプを取得します。
	/// </summary>
	public IMediaItemType GetMediaItemType(MediaType mediaType);

	/// <summary>
	/// レコードに対応するメディアアイテムタイプを取得します。
	/// </summary>
	public IMediaItemType GetMediaItemType(MediaItem MediaItem);

	/// <summary>
	/// パスがいずれかのメディアアイテムタイプの対象かどうかを取得します。
	/// </summary>
	public bool IsTargetPath(string path);

	/// <summary>
	/// 指定したメディアタイプのパス判定を行います。
	/// </summary>
	public bool IsTargetPath(string path, MediaType mediaType);

	/// <summary>
	/// メディアタイプ
	/// </summary>
	/// <param name="mediaType">メディアタイプ</param>
	/// <returns>実行プログラムオブジェクトモデル</returns>
	public IExecutionProgramObjectModel CreateExecutionProgramObjectModel(MediaType mediaType);

	/// <summary>
	/// モデルに対応する実行設定UIのビューモデルを作成します。
	/// </summary>
	/// <param name="model">モデル</param>
	/// <returns>実行プログラム設定ViewModel</returns>
	public IExecutionProgramConfigViewModel CreateExecutionConfigViewModel(IExecutionProgramObjectModel model);

	/// <summary>
	/// ビューモデルに対応する実行設定UIを作成します。
	/// </summary>
	public IExecutionConfigView CreateExecutionConfigView(IExecutionProgramConfigViewModel viewModel);
}