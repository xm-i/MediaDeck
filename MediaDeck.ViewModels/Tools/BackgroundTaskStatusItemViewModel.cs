using MediaDeck.Common.Base;

namespace MediaDeck.ViewModels.Tools;

/// <summary>
/// ステータスバーに表示するバックグラウンドタスク項目。
/// </summary>
public class BackgroundTaskStatusItemViewModel : ViewModelBase {
	/// <summary>
	/// バックグラウンドタスク項目を初期化する。
	/// </summary>
	/// <param name="displayName">表示名</param>
	/// <param name="completedCount">完了件数</param>
	/// <param name="targetCount">対象件数</param>
	/// <param name="reRun">再実行処理</param>
	/// <param name="cancel">キャンセル処理</param>
	public BackgroundTaskStatusItemViewModel(string displayName, BindableReactiveProperty<long> completedCount, BindableReactiveProperty<long> targetCount, Action reRun, Action? cancel = null) {
		this.DisplayName = displayName;
		this.CompletedCount = completedCount;
		this.TargetCount = targetCount;
		this.IsRunning = this.CompletedCount
			.CombineLatest(this.TargetCount, (completed, target) => target > 0 && completed < target)
			.ToBindableReactiveProperty()
			.AddTo(this.CompositeDisposable);
		this.ProgressText = this.CompletedCount
			.CombineLatest(this.TargetCount, (completed, target) => $"{completed}/{target}")
			.ToBindableReactiveProperty("0/0")
			.AddTo(this.CompositeDisposable);
		this.CompletedValue = this.CompletedCount
			.Select(x => (double)x)
			.ToBindableReactiveProperty()
			.AddTo(this.CompositeDisposable);
		this.TargetValue = this.TargetCount
			.Select(x => (double)x)
			.ToBindableReactiveProperty()
			.AddTo(this.CompositeDisposable);
		this.StatusText = this.IsRunning
			.Select(x => x ? "Running" : "Idle")
			.ToBindableReactiveProperty("Idle")
			.AddTo(this.CompositeDisposable);
		this.ReRunCommand = this.IsRunning
			.Select(x => !x)
			.ToReactiveCommand()
			.AddTo(this.CompositeDisposable);
		this.ReRunCommand.Subscribe(_ => reRun()).AddTo(this.CompositeDisposable);
		this.CancelCommand = this.IsRunning
			.ToReactiveCommand()
			.AddTo(this.CompositeDisposable);
		if (cancel != null) {
			this.CancelCommand.Subscribe(_ => cancel()).AddTo(this.CompositeDisposable);
		}
	}

	/// <summary>
	/// 表示名
	/// </summary>
	public string DisplayName {
		get;
	}

	/// <summary>
	/// 完了件数
	/// </summary>
	public BindableReactiveProperty<long> CompletedCount {
		get;
	}

	/// <summary>
	/// 対象件数
	/// </summary>
	public BindableReactiveProperty<long> TargetCount {
		get;
	}

	/// <summary>
	/// 実行中かどうか
	/// </summary>
	public BindableReactiveProperty<bool> IsRunning {
		get;
	}

	/// <summary>
	/// 進捗表示テキスト
	/// </summary>
	public BindableReactiveProperty<string> ProgressText {
		get;
	}

	/// <summary>
	/// 実行状態の表示テキスト
	/// </summary>
	public BindableReactiveProperty<string> StatusText {
		get;
	}

	/// <summary>
	/// タスク再実行コマンド
	/// </summary>
	public ReactiveCommand ReRunCommand {
		get;
	}

	/// <summary>
	/// タスクキャンセルコマンド
	/// </summary>
	public ReactiveCommand CancelCommand {
		get;
	}

	/// <summary>
	/// ProgressBar用の完了値
	/// </summary>
	public BindableReactiveProperty<double> CompletedValue {
		get;
	}

	/// <summary>
	/// ProgressBar用の最大値
	/// </summary>
	public BindableReactiveProperty<double> TargetValue {
		get;
	}
}