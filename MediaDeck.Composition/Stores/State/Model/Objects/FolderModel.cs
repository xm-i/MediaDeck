using R3.JsonConfig.Attributes;

namespace MediaDeck.Composition.Stores.State.Model.Objects;

[Inject(InjectServiceLifetime.Transient)]
[GenerateR3JsonConfigDefaultDto]
public class FolderModel() {
	public string FolderPath {
		get;
		set;
	} = string.Empty;
}
