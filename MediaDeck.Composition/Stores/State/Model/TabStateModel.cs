using R3.JsonConfig.Attributes;

namespace MediaDeck.Composition.Stores.State.Model;

/// <summary>
/// タブごとに独立する状態モデル
/// </summary>
[Inject(InjectServiceLifetime.Scoped)]
[GenerateR3JsonConfigDto]
public class TabStateModel(IServiceProvider serviceProvider, SearchStateModel searchState, ViewerStateModel viewerState) {
	public IServiceProvider ServiceProvider {
		get;
	} = serviceProvider;

	/// <summary>
	/// タブの表示名
	/// </summary>
	public ReactiveProperty<string> DisplayName {
		get;
		set;
	} = new("New Tab");

	public SearchStateModel SearchState {
		get;
		set;
	} = searchState;

	public ViewerStateModel ViewerState {
		get;
		set;
	} = viewerState;
}