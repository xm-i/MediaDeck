using MediaDeck.Common.Base;
using MediaDeck.Composition.Stores.Config.Model;

using Microsoft.Extensions.DependencyInjection;

namespace MediaDeck.ViewModels.Preferences.Config;

[Inject(InjectServiceLifetime.Transient)]
public class ExecutionConfigPageViewModel : ViewModelBase, IConfigPageViewModel {
	/// <summary>
	/// ページ名
	/// </summary>
	public string PageName {
		get;
	} = "外部プログラム";

	/// <summary>
	/// ページのアイコン（Segoe Fluent Icons のグリフ文字）
	/// </summary>
	public string PageIconGlyph {
		get;
	} = "\uE756";

	/// <summary>
	/// ページの説明
	/// </summary>
	public string PageDescription {
		get;
	} = "メディアを開く外部プログラムを設定します";


	private readonly ExecutionConfigModel _executionConfig;

	public ExecutionConfigPageViewModel(ExecutionConfigModel executionConfig) {
		this._executionConfig = executionConfig;
		this.AddExecutionProgramCommand.Subscribe(_ => {
			this._executionConfig.AddExecutionProgram();
		})
			.AddTo(this.CompositeDisposable);
		this.ExecutionPrograms =
			this._executionConfig
				.ExecutionPrograms
				.CreateView(x => x.ScopedServiceProvider.GetRequiredService<ExecutionProgramConfigViewModel>())
				.ToNotifyCollectionChanged();
	}

	/// <summary>
	/// 外部プログラムの一覧
	/// </summary>
	public INotifyCollectionChangedSynchronizedViewList<ExecutionProgramConfigViewModel> ExecutionPrograms {
		get;
	}

	/// <summary>
	/// プログラム追加コマンド
	/// </summary>
	public ReactiveCommand AddExecutionProgramCommand {
		get;
	} = new();
}