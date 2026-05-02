using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.MediaItemTypes.Models;
using MediaDeck.Composition.Interfaces.MediaItemTypes.ViewModels;
using MediaDeck.Composition.Tables;

namespace MediaDeck.Composition.Interfaces.MediaItemTypes;

public interface IMediaItemFactory<TFileOperator, TFileModel, TExecutionProgramObjectModel, TFileViewModel, TExecutionProgramConfigViewModel, TThumbnailPickerViewModel>
	: IMediaItemFactory
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

/// <summary>
/// 指定された ViewModel 型に対応するメディアアイテムファクトリ。
/// DI で正しいファクトリを注入するために使用する。
/// </summary>
/// <typeparam name="TViewModel">ViewModel の型。</typeparam>
public interface IMediaItemFactoryOf<TViewModel> : IMediaItemFactory where TViewModel : IMediaItemViewModel {
}

public interface IMediaItemFactory {
	public MediaType MediaType {
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

	/// <summary>
	/// このメディアタイプ用の一括サムネイル再生成設定 ViewModel を作成する。
	/// </summary>
	public IBulkThumbnailConfigViewModel CreateBulkThumbnailConfigViewModel();
}