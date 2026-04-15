using MediaDeck.Composition.Interfaces.FileTypes.Models;
using MediaDeck.Composition.Interfaces.FileTypes.ViewModels;
using MediaDeck.Composition.Interfaces.FileTypes.Views;
using MediaDeck.Database.Tables;

namespace MediaDeck.Composition.Interfaces.FileTypes;

/// <summary>
/// ファイルタイプに関連する操作を提供するサービスインターフェース
/// </summary>
public interface IFileTypeService {
	/// <summary>
	/// データベースレコードからファイルモデルを作成します。
	/// </summary>
	public IFileModel CreateFileModelFromRecord(MediaFile mediaFile);

	/// <summary>
	/// ファイルモデルからファイルビューモデルを作成します。
	/// </summary>
	public IFileViewModel CreateFileViewModel(IFileModel fileModel);

	/// <summary>
	/// ファイルビューモデルに対応する詳細プレビューコントロールビューを作成します。
	/// </summary>
	public IDetailViewerPreviewControlView CreateDetailViewerPreviewControlView(IFileViewModel fileViewModel);

	/// <summary>
	/// ファイルビューモデルに対応するサムネイル選択ビューモデルを作成します。
	/// </summary>
	public IThumbnailPickerViewModel CreateThumbnailPickerViewModel(IFileViewModel fileViewModel);

	/// <summary>
	/// ファイルビューモデルに対応するサムネイル選択ビューを作成します。
	/// </summary>
	public IThumbnailPickerView CreateThumbnailPickerView(IFileViewModel fileViewModel);

	/// <summary>
	/// 全てのファイルタイプに対するファイルオペレーターを作成します。
	/// </summary>
	public IFileOperator[] CreateFileOperators();

	/// <summary>
	/// クエリにファイルタイプ固有の関連テーブルを含めます。
	/// </summary>
	public IQueryable<MediaFile> IncludeTables(IQueryable<MediaFile> mediaFiles);
}
