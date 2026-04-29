using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.MediaItemTypes.Models;
using MediaDeck.Composition.Interfaces.MediaItemTypes.ViewModels;
using MediaDeck.Database.Tables;

namespace MediaDeck.Composition.Interfaces.MediaItemTypes;

public interface IMediaItemFactoryCore<TFileOperator, TFileModel, TExecutionProgramObjectModel, TFileViewModel, TExecutionProgramConfigViewModel, TThumbnailPickerViewModel> : IMediaItemFactoryCore
	where TFileOperator : IMediaItemOperator
	where TFileModel : IMediaItemModel
	where TExecutionProgramObjectModel : IExecutionProgramObjectModel
	where TFileViewModel : IMediaItemViewModel
	where TExecutionProgramConfigViewModel : IExecutionProgramConfigViewModel
	where TThumbnailPickerViewModel : IThumbnailPickerViewModel {
	public new TFileOperator CreateMediaItemOperator();
	public new TFileModel CreateMediaItemModelFromRecord(MediaItem MediaItem);
	public TFileViewModel CreateMediaItemViewModel(TFileModel fileModel);
	public new TThumbnailPickerViewModel CreateThumbnailPickerViewModel();
	/// <inheritdoc/>
	public new TExecutionProgramObjectModel CreateExecutionProgramObjectModel();

	/// <inheritdoc/>
	public TExecutionProgramConfigViewModel CreateExecutionProgramConfigViewModel(TExecutionProgramObjectModel model);
}

public interface IMediaItemFactoryCore {
	public MediaType MediaType {
		get;
	}

	public ItemType ItemType {
		get;
	}

	public IMediaItemOperator CreateMediaItemOperator();
	public IMediaItemModel CreateMediaItemModelFromRecord(MediaItem MediaItem);
	public IMediaItemViewModel CreateMediaItemViewModel(IMediaItemModel fileModel);
	public IThumbnailPickerViewModel CreateThumbnailPickerViewModel();
	/// <summary>
	/// このメディアタイプ用の実行設定オブジェクトを作成する。
	/// </summary>
	/// <returns>実行設定オブジェクト。</returns>
	public IExecutionProgramObjectModel CreateExecutionProgramObjectModel();

	/// <summary>
	/// このメディアタイプ用の実行設定 ViewModel を作成する。
	/// </summary>
	/// <param name="model">元となるモデルオブジェクト</param>
	/// <returns>ViewModel。</returns>
	public IExecutionProgramConfigViewModel CreateExecutionProgramConfigViewModel(IExecutionProgramObjectModel model);
}