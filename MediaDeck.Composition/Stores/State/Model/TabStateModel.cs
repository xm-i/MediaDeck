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
		var defaultSearch = defaultTab.SearchState;
		var defaultViewer = defaultTab.ViewerState;

		this.SearchState.CurrentSortCondition.Value = defaultSearch.CurrentSortCondition.Value;
		this.SearchState.SortDirection.Value = defaultSearch.SortDirection.Value;
		this.SearchState.CurrentFilteringConditions.Value = [.. defaultSearch.CurrentFilteringConditions.Value];
		this.ViewerState.ItemSize.Value = defaultViewer.ItemSize.Value;
		this.ViewerState.ActiveViewer.Value = defaultViewer.ActiveViewer.Value;
		this.ViewerState.ShowOverlay.Value = defaultViewer.ShowOverlay.Value;
		this.ViewerState.ShowInfo.Value = defaultViewer.ShowInfo.Value;

		// ListViewer 列設定
		this.ViewerState.ListFileNameColumnWidth.Value = defaultViewer.ListFileNameColumnWidth.Value;
		this.ViewerState.ListResolutionColumnWidth.Value = defaultViewer.ListResolutionColumnWidth.Value;
		this.ViewerState.ListFileSizeColumnWidth.Value = defaultViewer.ListFileSizeColumnWidth.Value;
		this.ViewerState.ListCreationTimeColumnWidth.Value = defaultViewer.ListCreationTimeColumnWidth.Value;
		this.ViewerState.ListRateColumnWidth.Value = defaultViewer.ListRateColumnWidth.Value;
		this.ViewerState.ListFileNameColumnVisible.Value = defaultViewer.ListFileNameColumnVisible.Value;
		this.ViewerState.ListResolutionColumnVisible.Value = defaultViewer.ListResolutionColumnVisible.Value;
		this.ViewerState.ListFileSizeColumnVisible.Value = defaultViewer.ListFileSizeColumnVisible.Value;
		this.ViewerState.ListCreationTimeColumnVisible.Value = defaultViewer.ListCreationTimeColumnVisible.Value;
		this.ViewerState.ListRateColumnVisible.Value = defaultViewer.ListRateColumnVisible.Value;

		this.ActiveRepository.Value = defaultTab.ActiveRepository.Value;

		this.LeftPaneWidth.Value = defaultTab.LeftPaneWidth.Value;
		this.RightPaneWidth.Value = defaultTab.RightPaneWidth.Value;
		this.RepositoryPaneHeight.Value = defaultTab.RepositoryPaneHeight.Value;
	}
}