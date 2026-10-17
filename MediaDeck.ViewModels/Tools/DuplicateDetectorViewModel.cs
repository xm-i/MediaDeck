using MediaDeck.Common.Base;
using MediaDeck.Common.Utilities;
using MediaDeck.Composition.Interfaces.MediaItemTypes;
using MediaDeck.Composition.Interfaces.MediaItemTypes.ViewModels;
using MediaDeck.Core.Primitives;
using MediaDeck.Core.Services.DuplicateFileDetector;

namespace MediaDeck.ViewModels.Tools;

/// <summary>
/// 重複検出ウィンドウViewModel
/// </summary>
[Inject(InjectServiceLifetime.Transient)]
public class DuplicateDetectorViewModel : ViewModelBase {
	private readonly DuplicateFileDetectorService _detector;
	private readonly IMediaItemTypeService _MediaItemTypeService;

	/// <summary>
	/// 重複ファイルグループリスト
	/// </summary>
	public INotifyCollectionChangedSynchronizedViewList<DuplicateFileGroup> DuplicateGroups {
		get;
	}

	/// <summary>
	/// 選択中グループ
	/// </summary>
	public BindableReactiveProperty<DuplicateFileGroup?> SelectedGroup {
		get;
	} = new();

	/// <summary>
	/// 選択中グループのファイルViewModelリスト
	/// </summary>
	public BindableReactiveProperty<IEnumerable<IMediaItemViewModel>?> SelectedGroupFiles {
		get;
	} = new();

	/// <summary>
	/// 選択中ファイル
	/// </summary>
	public BindableReactiveProperty<IMediaItemViewModel?> SelectedFile {
		get;
	} = new();

	/// <summary>
	/// 検出中フラグ
	/// </summary>
	public BindableReactiveProperty<bool> IsDetecting {
		get;
	}

	/// <summary>
	/// 検出完了フラグ
	/// </summary>
	public BindableReactiveProperty<bool> IsCompleted {
		get;
	}

	/// <summary>
	/// 重複グループ数
	/// </summary>
	public BindableReactiveProperty<int> DuplicateGroupCount {
		get;
	}

	/// <summary>
	/// 重複ファイル総数
	/// </summary>
	public BindableReactiveProperty<int> DuplicateFileCount {
		get;
	}

	/// <summary>
	/// ハッシュタイプ選択肢
	/// </summary>
	public IEnumerable<DisplayObject<bool>> HashTypeList {
		get;
	} = [
		new DisplayObject<bool>("FullHash", true),
		new DisplayObject<bool>("PreHash (First 1MB)", false)
	];

	/// <summary>
	/// 選択中ハッシュタイプ
	/// </summary>
	public BindableReactiveProperty<DisplayObject<bool>> SelectedHashType {
		get;
	} = new();

	/// <summary>
	/// 検出コマンド
	/// </summary>
	public ReactiveCommand DetectCommand {
		get;
	} = new();

	public DuplicateDetectorViewModel(DuplicateFileDetectorService detector, IMediaItemTypeService MediaItemTypeService) {
		this._detector = detector;
		this._MediaItemTypeService = MediaItemTypeService;

		this.DuplicateGroups = this._detector.DuplicateGroups.ToNotifyCollectionChanged(SynchronizationContextCollectionEventDispatcher.Current);

		this.IsDetecting = this._detector.IsDetecting.ToBindableReactiveProperty().AddTo(this.CompositeDisposable);
		this.IsCompleted = this._detector.IsCompleted.ToBindableReactiveProperty().AddTo(this.CompositeDisposable);
		this.DuplicateGroupCount = this._detector.DuplicateGroupCount.ToBindableReactiveProperty().AddTo(this.CompositeDisposable);
		this.DuplicateFileCount = this._detector.DuplicateFileCount.ToBindableReactiveProperty().AddTo(this.CompositeDisposable);

		this.SelectedHashType.Value = this.HashTypeList.First();

		this.SelectedGroup
			.Subscribe(g => {
				if (this.SelectedGroupFiles.Value != null) {
					foreach (var existing in this.SelectedGroupFiles.Value) {
						(existing as IDisposable)?.Dispose();
					}
				}
				this.SelectedGroupFiles.Value = g?.Files
					.Select(f => this._MediaItemTypeService.CreateMediaItemViewModel(this._MediaItemTypeService.CreateMediaItemModelFromRecord(f).AddTo(this.CompositeDisposable)))
					.ToArray();
			})
			.AddTo(this.CompositeDisposable);


		this.DetectCommand
			.SubscribeAwait(async (_, ct) => {
				await this._detector.DetectDuplicatesAsync(this.SelectedHashType.Value.Value);
			}, AwaitOperation.Drop)
			.AddTo(this.CompositeDisposable);
	}

	/// <summary>
	/// Explorerでファイルを表示
	/// </summary>
	public static void ShowInExplorer(string filePath) {
		ShellUtility.ShowInExplorer(filePath);
	}

	/// <inheritdoc/>
	protected override void Dispose(bool disposing) {
		if (disposing && this.SelectedGroupFiles.Value != null) {
			foreach (var existing in this.SelectedGroupFiles.Value) {
				(existing as IDisposable)?.Dispose();
			}
		}
		base.Dispose(disposing);
	}
}