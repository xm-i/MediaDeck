using MediaDeck.Composition.Enum;

using R3.JsonConfig.Attributes;

namespace MediaDeck.Composition.Stores.Config.Model.Objects;

[Inject(InjectServiceLifetime.Transient)]
[GenerateR3JsonConfigDto]
public class ExecutionProgramObjectModel {
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