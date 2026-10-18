using MediaDeck.Composition.Stores.State.Model;

namespace MediaDeck.ViewModels.Tabs;

/// <summary>
/// タブ固有のDIスコープ生成・初期化・破棄を一元管理するファクトリのインターフェース。
/// </summary>
public interface ITabScopeFactory {
	/// <summary>
	/// 新規タブ状態（<see cref="TabStateModel"/>）を専用DIスコープ内で生成し、
	/// デフォルトタブ設定の適用と一意な表示名の設定を行った上で <see cref="RootStateModel.Tabs"/> に追加します。
	/// </summary>
	/// <param name="displayName">指定する場合はタブの表示名。nullの場合は "Tab 1", "Tab 2" 等を自動採番</param>
	/// <returns>初期化済みの <see cref="TabStateModel"/></returns>
	public TabStateModel CreateTabState(string? displayName = null);

	/// <summary>
	/// 指定された <see cref="TabStateModel"/> に対応する <see cref="TabViewModel"/> を取得または生成します。
	/// 初回呼び出し時にタブ固有のDIスコープに対して通知コンテキスト等を初期化します。
	/// </summary>
	/// <param name="tabState">タブ状態モデル</param>
	/// <returns>構築された <see cref="TabViewModel"/></returns>
	public TabViewModel GetOrCreateTabViewModel(TabStateModel tabState);
}