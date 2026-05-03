using R3.JsonConfig.Attributes;

namespace MediaDeck.Composition.Stores.State.Model;

/// <summary>
/// 状態保存/復元のルートモデル（AppState + ウィンドウ状態の配列）
/// </summary>
[Inject(InjectServiceLifetime.Transient)]
[GenerateR3JsonConfigDto]
public class RootStateModel {
	/// <summary>
	/// 状態バージョン
	/// </summary>
	public int Version {
		get;
		set;
	} = 1;

	/// <summary>
	/// アプリ全体の共有状態
	/// </summary>
	public AppStateModel AppState {
		get;
		set;
	}

	/// <summary>
	/// タブごとの状態リスト（ルートスコープから生成）
	/// </summary>
	[JsonConfigCreateScope]
	public ObservableList<TabStateModel> Tabs {
		get;
	} = [];

	/// <summary>
	/// ウィンドウごとの状態リスト
	/// </summary>
	[JsonConfigCreateScope]
	public ObservableList<WindowStateModel> Windows {
		get;
	} = [];

	public RootStateModel(AppStateModel appState) {
		this.AppState = appState;
	}
}