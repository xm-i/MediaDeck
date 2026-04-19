using R3.JsonConfig.Attributes;

namespace MediaDeck.Composition.Stores.State.Model;

/// <summary>
/// 状態保存/復元のルートモデル（AppState + タブ状態の配列）
/// </summary>
[Inject(InjectServiceLifetime.Transient)]
[GenerateR3JsonConfigDto]
public class RootStateModel {
	/// <summary>
	/// アプリ全体の共有状態
	/// </summary>
	public AppStateModel AppState {
		get;
		set;
	}

	/// <summary>
	/// タブごとの状態リスト
	/// </summary>
	public ObservableList<TabStateModel> Tabs {
		get;
	} = [];

	/// <summary>
	/// アクティブだったタブのインデックス
	/// </summary>
	public int ActiveTabIndex {
		get;
		set;
	}

	public RootStateModel(AppStateModel appState) {
		this.AppState = appState;
	}

	public RootStateModel(AppStateModel appState, ObservableList<TabStateModel> tabs, int activeTabIndex) {
		this.AppState = appState;
		this.Tabs = tabs;
		this.ActiveTabIndex = activeTabIndex;
	}
}