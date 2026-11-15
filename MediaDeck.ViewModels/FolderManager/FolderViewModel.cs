using MediaDeck.Common.Base;
using MediaDeck.Composition.Stores.Config.Model.Objects;

namespace MediaDeck.ViewModels.FolderManager;

/// <summary>
/// 登録フォルダー項目のViewModelを表します。
/// </summary>
[Inject(InjectServiceLifetime.Transient)]
public class FolderViewModel : ViewModelBase {
	private readonly FolderModel _folderModel;

	/// <summary>
	/// フォルダーパスを取得します。
	/// </summary>
	public string FolderPath {
		get {
			return this._folderModel.FolderPath;
		}
	}

	/// <summary>
	/// フォルダーがスキャン中であるかどうかを取得します。
	/// </summary>
	public BindableReactiveProperty<bool> IsScanning {
		get;
	}

	/// <summary>
	/// 配下フォルダーをグループ化するかどうかを取得または設定します。
	/// </summary>
	public BindableReactiveProperty<bool> IsGroupingRoot {
		get;
	}

	/// <summary>
	/// スキャン対象の総ファイル件数を取得します。
	/// </summary>
	public BindableReactiveProperty<long> TotalCount {
		get;
	}

	/// <summary>
	/// スキャン処理の残りファイル件数を取得します。
	/// </summary>
	public BindableReactiveProperty<long> RemainingCount {
		get;
	}

	/// <summary>
	/// 処理済みのファイル件数を取得します。
	/// </summary>
	public BindableReactiveProperty<long> ProcessedCount {
		get;
	}

	/// <summary>
	/// <see cref="FolderViewModel"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	/// <param name="folderModel">対象のフォルダーモデル</param>
	public FolderViewModel(FolderModel folderModel) {
		this._folderModel = folderModel;

		this.IsScanning = folderModel.IsScanning
			.ObserveOnCurrentSynchronizationContext()
			.ToBindableReactiveProperty(folderModel.IsScanning.Value)
			.AddTo(this.CompositeDisposable);

		this.IsGroupingRoot = new BindableReactiveProperty<bool>(folderModel.IsGroupingRoot)
			.AddTo(this.CompositeDisposable);
		this.IsGroupingRoot.Subscribe(x => folderModel.IsGroupingRoot = x).AddTo(this.CompositeDisposable);

		this.TotalCount = folderModel.TotalCount
			.ObserveOnCurrentSynchronizationContext()
			.ToBindableReactiveProperty(folderModel.TotalCount.Value)
			.AddTo(this.CompositeDisposable);

		this.RemainingCount = folderModel.RemainingCount
			.ThrottleLast(TimeSpan.FromMilliseconds(50))
			.ObserveOnCurrentSynchronizationContext()
			.ToBindableReactiveProperty(folderModel.RemainingCount.Value)
			.AddTo(this.CompositeDisposable);

		this.ProcessedCount = folderModel.TotalCount
			.CombineLatest(folderModel.RemainingCount, (total, remaining) => Math.Max(0, total - remaining))
			.ThrottleLast(TimeSpan.FromMilliseconds(50))
			.CombineLatest(folderModel.IsScanning, (processed, isScanning) => isScanning ? processed : folderModel.TotalCount.Value)
			.ObserveOnCurrentSynchronizationContext()
			.ToBindableReactiveProperty(Math.Max(0, folderModel.TotalCount.Value - folderModel.RemainingCount.Value))
			.AddTo(this.CompositeDisposable);
	}

	/// <summary>
	/// 内部のフォルダーモデルを取得します。
	/// </summary>
	/// <returns>フォルダーモデル</returns>
	public FolderModel GetModel() {
		return this._folderModel;
	}
}