using R3.JsonConfig.Attributes;

namespace MediaDeck.Composition.Stores.State.Model.Objects;

[AddTransient]
[GenerateR3JsonConfigDefaultDto]
public class FolderModel() {
	public string FolderPath {
		get;
		set;
	} = string.Empty;
}
