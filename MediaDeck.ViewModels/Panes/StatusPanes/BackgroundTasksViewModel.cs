using MediaDeck.Common.Base;
using MediaDeck.Core.Models.Tools;

namespace MediaDeck.ViewModels.Panes.StatusPanes;

/// <summary>
/// バックグラウンドタスク表示ViewModel。
/// </summary>
[Inject(InjectServiceLifetime.Singleton)]
public class BackgroundTasksViewModel : ViewModelBase {
	private readonly BackgroundTasksModel _model;

	/// <summary>
	/// バックグラウンドタスク表示ViewModelを初期化する。
	/// </summary>
	/// <param name="model">モデル</param>
	public BackgroundTasksViewModel(BackgroundTasksModel model) {
		this._model = model;

		this.TaskItems = this._model.TaskItems
			.Select(x => new BackgroundTaskStatusItemViewModel(x).AddTo(this.CompositeDisposable))
			.ToList();

		var runningStatusChanged = Observable.Merge(this.TaskItems.Select(x => x.IsRunning.Select(_ => Unit.Default)));
		this.RunningTaskItems = runningStatusChanged
			.Select(_ => this.GetRunningTaskItems())
			.ToBindableReactiveProperty(this.GetRunningTaskItems())
			.AddTo(this.CompositeDisposable);
		this.ActiveTaskCount = runningStatusChanged
			.Select(_ => this.TaskItems.Count(x => x.IsRunning.Value))
			.ToBindableReactiveProperty(this.TaskItems.Count(x => x.IsRunning.Value))
			.AddTo(this.CompositeDisposable);
		this.HasRunningTasks = this.ActiveTaskCount
			.Select(x => x > 0)
			.ToBindableReactiveProperty()
			.AddTo(this.CompositeDisposable);
		this.SummaryText = this.ActiveTaskCount
			.Select(count => count > 0 ? $"Running" : "Idle")
			.ToBindableReactiveProperty("Idle")
			.AddTo(this.CompositeDisposable);
	}

	/// <summary>
	/// ステータス表示用タスク一覧
	/// </summary>
	public IReadOnlyList<BackgroundTaskStatusItemViewModel> TaskItems {
		get;
	}

	/// <summary>
	/// 実行中のタスク一覧
	/// </summary>
	public BindableReactiveProperty<IReadOnlyList<BackgroundTaskStatusItemViewModel>> RunningTaskItems {
		get;
	}

	/// <summary>
	/// 実行中タスク数
	/// </summary>
	public BindableReactiveProperty<int> ActiveTaskCount {
		get;
	}

	/// <summary>
	/// 実行中タスクがあるかどうか
	/// </summary>
	public BindableReactiveProperty<bool> HasRunningTasks {
		get;
	}

	/// <summary>
	/// ステータスバーに表示する概要テキスト
	/// </summary>
	public BindableReactiveProperty<string> SummaryText {
		get;
	}

	/// <summary>
	/// 実行中のタスク一覧を取得する。
	/// </summary>
	/// <returns>実行中タスク一覧</returns>
	private IReadOnlyList<BackgroundTaskStatusItemViewModel> GetRunningTaskItems() {
		return [.. this.TaskItems.Where(x => x.IsRunning.Value)];
	}
}