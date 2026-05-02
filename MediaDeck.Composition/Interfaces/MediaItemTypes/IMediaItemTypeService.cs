using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.MediaItemTypes.Models;
using MediaDeck.Composition.Interfaces.MediaItemTypes.ViewModels;
using MediaDeck.Composition.Tables;

namespace MediaDeck.Composition.Interfaces.MediaItemTypes;

/// <summary>
/// メディアアイテムタイプに関連する操作を提供するサービスインターフェース
/// </summary>
public interface IMediaItemTypeService {
	/// <summary>
	/// データベースレコードからメディアアイテムモデルを作成します。
	/// </summary>
	public IMediaItemModel CreateMediaItemModelFromRecord(MediaItem MediaItem);

	/// <summary>
	/// メディアアイテムモデルからビューモデルを作成します。
	/// </summary>
	public IMediaItemViewModel CreateMediaItemViewModel(IMediaItemModel fileModel);

	/// <summary>
	/// ビューモデルに対応するサムネイル選択ビューモデルを作成します。
	/// </summary>
	public IThumbnailPickerViewModel CreateThumbnailPickerViewModel(IMediaItemViewModel fileViewModel);

	/// <summary>
	/// 全てのメディアアイテムタイプに対するオペレーターを作成します。
	/// </summary>
	public IMediaItemOperator[] CreateMediaItemOperators();

	/// <summary>
	/// クエリにメディアアイテムタイプ固有の関連テーブルを含めます。
	/// </summary>
	public IQueryable<MediaItem> IncludeTables(IQueryable<MediaItem> MediaItems);

	/// <summary>
	/// パスに対応するメディアアイテムFactoryを取得します。
	/// </summary>
	public IMediaItemFactory GetMediaItemFactory(string path);

	/// <summary>
	/// 指定したメディアタイプに対応するメディアアイテムFactoryを取得します。
	/// </summary>
	public IMediaItemFactory GetMediaItemFactory(MediaType mediaType);

	/// <summary>
	/// レコードに対応するメディアアイテムFactoryを取得します。
	/// </summary>
	public IMediaItemFactory GetMediaItemFactory(MediaItem MediaItem);

	/// <summary>
	/// 指定したメディアタイプに対応するメディアアイテムタイププロバイダーを取得します。
	/// </summary>
	public IMediaItemTypeProvider GetMediaItemTypeProvider(MediaType mediaType);
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
	/// 指定したメディアタイプの一括サムネイル再生成設定ViewModelを作成します。
	/// </summary>
	public IBulkThumbnailConfigViewModel CreateBulkThumbnailConfigViewModel(MediaType mediaType);


}