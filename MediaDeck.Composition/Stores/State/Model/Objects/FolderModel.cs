using System.Text.Json.Serialization;

using R3.JsonConfig.Attributes;

namespace MediaDeck.Composition.Stores.State.Model.Objects;

[Inject(InjectServiceLifetime.Transient)]
[GenerateR3JsonConfigDto]
public class FolderModel() {
	public string FolderPath {
		get;
		set;
	} = string.Empty;

	[ExcludeProperty]
	public ReactiveProperty<bool> IsScanning {
		get;
	} = new(false);

	[ExcludeProperty]
	public ReactiveProperty<long> TotalCount {
		get;
	} = new(0);

	[ExcludeProperty]
	public ReactiveProperty<long> RemainingCount {
		get;
	} = new(0);
}
