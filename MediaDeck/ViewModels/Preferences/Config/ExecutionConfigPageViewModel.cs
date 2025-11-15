using MediaDeck.Composition.Bases;
using MediaDeck.Composition.Stores.Config.Model;

using Microsoft.Extensions.DependencyInjection;

namespace MediaDeck.ViewModels.Preferences.Config;

[AddTransient]
public class ExecutionConfigPageViewModel : ViewModelBase, IConfigPageViewModel {
	public string PageName {
		get;
	} = "Execution";

	private readonly ExecutionConfigModel _executionConfig;
	public ExecutionConfigPageViewModel(ExecutionConfigModel executionConfig) {
		this._executionConfig = executionConfig;
		this.AddExecutionProgramCommand.Subscribe(_ => {
			this._executionConfig.AddExecutionProgram();
		});
		this.ExecutionPrograms =
			this._executionConfig
				.ExecutionPrograms
				.CreateView(x => x.ScopedServiceProvider.GetRequiredService<ExecutionProgramConfigViewModel>())
				.ToNotifyCollectionChanged();
	}

	/// <summary>
	/// 対象拡張子
	/// </summary>
	public INotifyCollectionChangedSynchronizedViewList<ExecutionProgramConfigViewModel> ExecutionPrograms {
		get;
	}

	public ReactiveCommand AddExecutionProgramCommand {
		get;
	} = new();
}
