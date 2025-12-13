using MediaDeck.Composition.Enum;

using R3.JsonConfig.Attributes;

namespace MediaDeck.Composition.Stores.Config.Model.Objects;

[Inject(InjectServiceLifetime.Scoped)]
[GenerateR3JsonConfigDefaultDto]
public class ExtensionObjectModel {
	public IServiceProvider ScopedServiceProvider {
		get;
	}

	public ExtensionObjectModel(IServiceProvider serviceProvider) {
		this.ScopedServiceProvider = serviceProvider;
	}

	public ReactiveProperty<string> Extension {
		get;
		set;
	} = new();

	public ReactiveProperty<MediaType> MediaType {
		get;
		set;
	} = new();
}
