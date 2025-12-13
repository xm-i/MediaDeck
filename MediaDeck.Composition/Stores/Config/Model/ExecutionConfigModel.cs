using MediaDeck.Composition.Stores.Config.Model.Objects;

using Microsoft.Extensions.DependencyInjection;

using R3.JsonConfig.Attributes;

namespace MediaDeck.Composition.Stores.Config.Model;

[Inject(InjectServiceLifetime.Singleton)]
[GenerateR3JsonConfigDefaultDto]
public class ExecutionConfigModel {
	public IServiceProvider ScopedServiceProvider {
		get;
	}

	public ExecutionConfigModel(IServiceProvider serviceProvider) {
		this.ScopedServiceProvider = serviceProvider;
	}

	/// <summary>
	/// 実行プログラム
	/// </summary>

	public ObservableList<ExecutionProgramObjectModel> ExecutionPrograms {
		get;
	} = [];

	public void AddExecutionProgram() {
		var scope = this.ScopedServiceProvider.CreateScope();
		var programConfig = scope.ServiceProvider.GetRequiredService<ExecutionProgramObjectModel>();
		this.ExecutionPrograms.Add(programConfig);
	}

	public void RemoveExecutionProgram(ExecutionProgramObjectModel program) {
		this.ExecutionPrograms.Remove(program);
	}
}
