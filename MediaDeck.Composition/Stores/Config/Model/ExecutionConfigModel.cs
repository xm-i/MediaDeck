using MediaDeck.Composition.Stores.Config.Model.Objects;

using Microsoft.Extensions.DependencyInjection;

using R3.JsonConfig.Attributes;

namespace MediaDeck.Composition.Stores.Config.Model;

[Inject(InjectServiceLifetime.Singleton)]
[GenerateR3JsonConfigDto]
public class ExecutionConfigModel {
	private readonly IServiceProvider _serviceProvider;

	public ExecutionConfigModel(IServiceProvider serviceProvider) {
		this._serviceProvider = serviceProvider;
	}

	/// <summary>
	/// 実行プログラム
	/// </summary>

	public ObservableList<ExecutionProgramObjectModel> ExecutionPrograms {
		get;
	} = [];

	public void AddExecutionProgram() {
		var programConfig = this._serviceProvider.GetRequiredService<ExecutionProgramObjectModel>();
		this.ExecutionPrograms.Add(programConfig);
	}

	public void RemoveExecutionProgram(ExecutionProgramObjectModel program) {
		this.ExecutionPrograms.Remove(program);
	}
}