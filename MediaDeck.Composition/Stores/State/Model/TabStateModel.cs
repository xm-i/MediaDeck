using GenJsonConfig.Attributes;

namespace MediaDeck.Composition.Stores.State.Model;

/// <summary>
/// タブごとに独立する状態モデル
/// </summary>
[Inject(InjectServiceLifetime.Scoped)]
[GenerateJsonConfigDto]
public class TabStateModel(IServiceProvider serviceProvider, SearchStateModel searchState, ViewerStateModel viewerState) {
	/// <summary>
	/// タブ固有のDIスコープのサービスプロバイダー
	/// </summary>
	public IServiceProvider ServiceProvider {
		get;
	} = serviceProvider;
	/// <summary>
	/// タブの一意識別子
	/// </summary>
	public Guid TabId {
		get;
		set;
	} = Guid.NewGuid();

	/// <summary>
	/// タブの表示名
	/// </summary>
	public ReactiveProperty<string> DisplayName {
		get;
		set;
	} = new();

	public SearchStateModel SearchState {
		get;
		set;
	} = searchState;

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

	/// <summary>
	///     デフォルトのタブ状態から設定値をコピーして初期化します。
	/// </summary>
	/// <param name="defaultTab">デフォルトのタブ状態モデル</param>
	public void ApplyDefaultState(DefaultTabStateModel defaultTab) {
		this.SearchState.CopyFrom(defaultTab.SearchState);
		this.ViewerState.CopyFrom(defaultTab.ViewerState);

		this.ActiveRepository.Value = defaultTab.ActiveRepository.Value;
		this.LeftPaneWidth.Value = defaultTab.LeftPaneWidth.Value;
		this.RightPaneWidth.Value = defaultTab.RightPaneWidth.Value;
		this.RepositoryPaneHeight.Value = defaultTab.RepositoryPaneHeight.Value;
	}
}