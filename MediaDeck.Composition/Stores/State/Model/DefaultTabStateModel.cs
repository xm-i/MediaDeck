using GenJsonConfig.Attributes;

namespace MediaDeck.Composition.Stores.State.Model;

/// <summary>
/// 新規タブのデフォルト状態テンプレート（Singleton）
/// </summary>
[Inject(InjectServiceLifetime.Singleton)]
[GenerateJsonConfigDto]
public class DefaultTabStateModel(SearchStateModel searchState, ViewerStateModel viewerState) {
	/// <summary>
	/// 検索状態テンプレート
	/// </summary>
	public SearchStateModel SearchState {
		get;
		set;
	} = searchState;

	/// <summary>
	/// ビューワー状態テンプレート
	/// </summary>
	public ViewerStateModel ViewerState {
		get;
		set;
	} = viewerState;

	/// <summary>
	/// 現在選択されているリポジトリの種類
	/// </summary>
	public ReactiveProperty<RepositoryType> ActiveRepository {
		get;
		set;
	} = new(RepositoryType.Folder);

	/// <summary>
	/// 左ペインの幅
	/// </summary>
	public ReactiveProperty<double> LeftPaneWidth {
		get;
		set;
	} = new(250);

	/// <summary>
	/// 右ペインの幅
	/// </summary>
	public ReactiveProperty<double> RightPaneWidth {
		get;
		set;
	} = new(250);

	/// <summary>
	/// Repository ペインの高さ
	/// </summary>
	public ReactiveProperty<double> RepositoryPaneHeight {
		get;
		set;
	} = new(250);
}