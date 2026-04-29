using MediaDeck.Composition.Interfaces.MediaItemTypes.Models;
using MediaDeck.Composition.Interfaces.MediaItemTypes.ViewModels;
using MediaDeck.Composition.Interfaces.MediaItemTypes.Views;

namespace MediaDeck.Composition.Interfaces.MediaItemTypes;

public interface IMediaItemFactory<TFileOperator, TFileModel, TExecutionProgramObjectModel, TFileViewModel, TExecutionProgramConfigViewModel, TDetailViewerPreviewControlView, TThumbnailPickerViewModel, TThumbnailPickerView, TExecutionConfigView>
	: IMediaItemFactory, IMediaItemFactoryCore<TFileOperator, TFileModel, TExecutionProgramObjectModel, TFileViewModel, TExecutionProgramConfigViewModel, TThumbnailPickerViewModel>
	where TFileOperator : IMediaItemOperator
	where TFileModel : IMediaItemModel
	where TExecutionProgramObjectModel : IExecutionProgramObjectModel
	where TFileViewModel : IMediaItemViewModel
	where TExecutionProgramConfigViewModel : IExecutionProgramConfigViewModel
	where TDetailViewerPreviewControlView : IDetailViewerPreviewControlView
	where TThumbnailPickerViewModel : IThumbnailPickerViewModel
	where TThumbnailPickerView : IThumbnailPickerView
	where TExecutionConfigView : IExecutionConfigView {
	public TDetailViewerPreviewControlView CreateDetailViewerPreviewControlView(TFileViewModel fileViewModel);
	public IThumbnailControlView CreateThumbnailControlView(TFileViewModel fileViewModel);
	public new TThumbnailPickerView CreateThumbnailPickerView();
	/// <inheritdoc/>
	public TExecutionConfigView CreateExecutionConfigView(TExecutionProgramConfigViewModel viewModel);
}

/// <summary>
/// 指定された ViewModel 型に対応するメディアアイテムファクトリ。
/// DI で正しいファクトリを注入するために使用する。
/// </summary>
/// <typeparam name="TViewModel">ViewModel の型。</typeparam>
public interface IMediaItemFactoryOf<TViewModel> : IMediaItemFactory where TViewModel : IMediaItemViewModel {
}

public interface IMediaItemFactory : IMediaItemFactoryCore {
	public IDetailViewerPreviewControlView CreateDetailViewerPreviewControlView(IMediaItemViewModel fileViewModel);
	public IThumbnailControlView CreateThumbnailControlView(IMediaItemViewModel fileViewModel);
	public IThumbnailPickerView CreateThumbnailPickerView();
	/// <summary>
	/// このメディアタイプの実行設定UIを作成する。
	/// </summary>
	/// <param name="viewModel">View にセットする ViewModel</param>
	/// <returns>実行設定UIのビュー。</returns>
	public IExecutionConfigView CreateExecutionConfigView(IExecutionProgramConfigViewModel viewModel);
}