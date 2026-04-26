using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.MediaItemTypes.Models;
using MediaDeck.Composition.Interfaces.MediaItemTypes.ViewModels;
using MediaDeck.Composition.Interfaces.MediaItemTypes.Views;
using MediaDeck.Database.Tables;

namespace MediaDeck.Composition.Interfaces.MediaItemTypes;

public interface IMediaItemType<TFileOperator, TFileModel, TExecutionProgramObjectModel, TFileViewModel, TExecutionProgramConfigViewModel, TDetailViewerPreviewControlView, TThumbnailPickerViewModel, TThumbnailPickerView, TExecutionConfigView> : IMediaItemType
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

public interface IMediaItemType {
	public MediaType MediaType {
		get;
	}

	public ItemType ItemType {
		get;
	}

	public IMediaItemOperator CreateMediaItemOperator();
	public IMediaItemModel CreateMediaItemModelFromRecord(MediaItem MediaItem, IServiceProvider scopedServiceProvider);
	public IDetailViewerPreviewControlView CreateDetailViewerPreviewControlView(IMediaItemViewModel fileViewModel);
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
	public IQueryable<MediaItem> IncludeTables(IQueryable<MediaItem> MediaItems);
	public bool IsTargetPath(string path);
	public MediaItemPathStatus GetPathStatus(string path);

	/// <summary>
	/// 指定されたファイルパスに対してこのメディアタイプ固有の実行処理を行う。
	/// </summary>
	/// <param name="filePath">実行対象のファイルパス</param>
	/// <param name="scopedServiceProvider">実行するタブのスコープを切ったサービスプロバイダー</param>
	/// <returns>非同期タスク</returns>
	public Task ExecuteAsync(string filePath, IServiceProvider scopedServiceProvider);
}

public readonly struct MediaItemPathStatus {
	public MediaItemPathStatus(bool exists, long fileSize, DateTime creationTime, DateTime modifiedTime, DateTime lastAccessTime) {
		this.Exists = exists;
		this.FileSize = fileSize;
		this.CreationTime = creationTime;
		this.ModifiedTime = modifiedTime;
		this.LastAccessTime = lastAccessTime;
	}

	public bool Exists {
		get;
	}

	public long FileSize {
		get;
	}

	public DateTime CreationTime {
		get;
	}

	public DateTime ModifiedTime {
		get;
	}

	public DateTime LastAccessTime {
		get;
	}
}