using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.MediaItemTypes.Models;
using MediaDeck.Composition.Interfaces.MediaItemTypes.ViewModels;
using MediaDeck.Composition.Interfaces.MediaItemTypes.Views;
using MediaDeck.Database.Tables;

namespace MediaDeck.Composition.Interfaces.MediaItemTypes;

public interface IMediaItemFactory<TFileOperator, TFileModel, TExecutionProgramObjectModel, TFileViewModel, TExecutionProgramConfigViewModel, TDetailViewerPreviewControlView, TThumbnailPickerViewModel, TThumbnailPickerView, TExecutionConfigView> : IMediaItemFactory
	where TFileOperator : IMediaItemOperator
	where TFileModel : IMediaItemModel
	where TExecutionProgramObjectModel : IExecutionProgramObjectModel
	where TFileViewModel : IMediaItemViewModel
	where TExecutionProgramConfigViewModel : IExecutionProgramConfigViewModel
	where TDetailViewerPreviewControlView : IDetailViewerPreviewControlView
	where TThumbnailPickerViewModel : IThumbnailPickerViewModel
	where TThumbnailPickerView : IThumbnailPickerView
	where TExecutionConfigView : IExecutionConfigView {
	public new TFileOperator CreateMediaItemOperator();
	public new TFileModel CreateMediaItemModelFromRecord(MediaItem MediaItem, IServiceProvider scopedServiceProvider);
	public TDetailViewerPreviewControlView CreateDetailViewerPreviewControlView(TFileViewModel fileViewModel);
	public IThumbnailControlView CreateThumbnailControlView(TFileViewModel fileViewModel);
	public TFileViewModel CreateMediaItemViewModel(TFileModel fileModel);
	public new TThumbnailPickerViewModel CreateThumbnailPickerViewModel();
	public new TThumbnailPickerView CreateThumbnailPickerView();

	/// <inheritdoc/>
	public new TExecutionProgramObjectModel CreateExecutionProgramObjectModel();

	/// <inheritdoc/>
	public TExecutionProgramConfigViewModel CreateExecutionProgramConfigViewModel(TExecutionProgramObjectModel model);
	/// <inheritdoc/>
	public TExecutionConfigView CreateExecutionConfigView(TExecutionProgramConfigViewModel viewModel);
}

public interface IMediaItemFactory {
	public MediaType MediaType {
		get;
	}

	public ItemType ItemType {
		get;
	}

	public IMediaItemOperator CreateMediaItemOperator();
	public IMediaItemModel CreateMediaItemModelFromRecord(MediaItem MediaItem, IServiceProvider scopedServiceProvider);
	public IDetailViewerPreviewControlView CreateDetailViewerPreviewControlView(IMediaItemViewModel fileViewModel);
	public IThumbnailControlView CreateThumbnailControlView(IMediaItemViewModel fileViewModel);
	public IMediaItemViewModel CreateMediaItemViewModel(IMediaItemModel fileModel);
	public IThumbnailPickerViewModel CreateThumbnailPickerViewModel();
	public IThumbnailPickerView CreateThumbnailPickerView();
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
	/// このメディアタイプの実行設定UIを作成する。
	/// </summary>
	/// <param name="viewModel">View にセットする ViewModel</param>
	/// <returns>実行設定UIのビュー。</returns>
	public IExecutionConfigView CreateExecutionConfigView(IExecutionProgramConfigViewModel viewModel);
}