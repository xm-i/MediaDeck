using MediaDeck.Composition.Enum;

using R3.JsonConfig.Attributes;

namespace MediaDeck.Composition.Stores.Config.Model.Objects;

[AddScoped]
[GenerateR3JsonConfigDefaultDto]
public class ExecutionProgramObjectModel {
	public IServiceProvider ScopedServiceProvider {
		get;
	}

	public ExecutionProgramObjectModel(IServiceProvider serviceProvider) {
		this.ScopedServiceProvider = serviceProvider;
	}

	public ReactiveProperty<string> Path {
		get;
		set;
	} = new();

	public ReactiveProperty<string> Args {
		get;
		set;
	} = new();

	public ReactiveProperty<MediaType> MediaType {
		get;
		set;
	} = new();

}
