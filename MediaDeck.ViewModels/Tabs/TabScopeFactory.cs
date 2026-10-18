using System.Collections.Concurrent;
using MediaDeck.Common.Base;
using MediaDeck.Composition.Objects;
using MediaDeck.Composition.Stores.State.Model;
using MediaDeck.Core.Stores.State;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MediaDeck.ViewModels.Tabs;

/// <summary>
/// タブ固有のDIスコープ生成・初期化・破棄を一元管理するファクトリ実装。
/// <see cref="RootStateModel.Tabs"/> からの削除を監視し、対応する <see cref="TabViewModel"/> とDIスコープを自動的に破棄します。
/// </summary>
[Inject(InjectServiceLifetime.Singleton, typeof(ITabScopeFactory))]
public class TabScopeFactory : DisposableBase, ITabScopeFactory {
	private readonly IServiceProvider _rootServiceProvider;
	private readonly IStateStore _stateStore;
	private readonly ILogger<TabScopeFactory> _logger;
	private readonly ConcurrentDictionary<Guid, TabViewModel> _tabViewModels = new();

	/// <summary>
	/// <see cref="TabScopeFactory"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	/// <param name="rootServiceProvider">ルートサービスプロバイダー</param>
	/// <param name="stateStore">状態ストア</param>
	/// <param name="logger">ロガー</param>
	public TabScopeFactory(
		IServiceProvider rootServiceProvider,
		IStateStore stateStore,
		ILogger<TabScopeFactory> logger) {
		this._rootServiceProvider = rootServiceProvider;
		this._stateStore = stateStore;
		this._logger = logger;

		// RootState.Tabs からタブが削除された際に、対応する ViewModel と DI スコープをリアクティブに破棄する
		this._stateStore.RootState.Tabs.ObserveRemove()
			.Subscribe(x => this.OnTabStateRemoved(x.Value))
			.AddTo(this.CompositeDisposable);
	}

	/// <inheritdoc />
	public TabStateModel CreateTabState(string? displayName = null) {
		var scope = this._rootServiceProvider.CreateScope();
		try {
			var tabState = scope.ServiceProvider.GetRequiredService<TabStateModel>();

			if (string.IsNullOrWhiteSpace(displayName)) {
				for (var num = 1; true; num++) {
					var candidateName = $"Tab {num}";
					if (this._stateStore.RootState.Tabs.All(t => t.DisplayName.Value != candidateName)) {
						tabState.DisplayName.Value = candidateName;
						break;
					}
				}
			} else {
				tabState.DisplayName.Value = displayName;
			}

			var defaultTab = this._stateStore.RootState.AppState.DefaultTabState;
			tabState.ApplyDefaultState(defaultTab);

			this._stateStore.RootState.Tabs.Add(tabState);
			return tabState;
		} catch (Exception ex) {
			this._logger.LogError(ex, "新規タブの生成に失敗しました");
			scope.Dispose();
			throw;
		}
	}

	/// <inheritdoc />
	public TabViewModel GetOrCreateTabViewModel(TabStateModel tabState) {
		if (this._tabViewModels.TryGetValue(tabState.TabId, out var existing)) {
			return existing;
		}

		var scopeProvider = tabState.ServiceProvider;

		// 通知コンテキストの初期化（所属するWindowIdを動的に検索）
		var notifContext = scopeProvider.GetRequiredService<NotificationContextProvider>();
		notifContext.TargetWindowIdResolver = () => {
			return this._stateStore.RootState.Windows.FirstOrDefault(w => w.TabIds.Contains(tabState.TabId))?.WindowId;
		};

		// タブ固有のDIスコープから TabViewModel を解決
		var tabViewModel = scopeProvider.GetRequiredService<TabViewModel>();
		this._tabViewModels[tabState.TabId] = tabViewModel;
		this._logger.LogInformation("TabViewModelを生成・初期化しました: TabId={TabId}", tabState.TabId);

		return tabViewModel;
	}

	/// <summary>
	/// <see cref="RootStateModel.Tabs"/> からタブ状態が削除された際に対応する <see cref="TabViewModel"/> とDIスコープを破棄します。
	/// </summary>
	/// <param name="tabState">削除されたタブ状態モデル</param>
	private void OnTabStateRemoved(TabStateModel tabState) {
		if (this._tabViewModels.TryRemove(tabState.TabId, out var viewModel)) {
			try {
				viewModel.Dispose();
			} catch (Exception ex) {
				this._logger.LogError(ex, "TabViewModelの破棄中にエラーが発生しました: TabId={TabId}", tabState.TabId);
			}
		}

		try {
			(tabState.ServiceProvider as IDisposable)?.Dispose();
			this._logger.LogInformation("タブスコープを破棄しました: TabId={TabId}", tabState.TabId);
		} catch (Exception ex) {
			this._logger.LogError(ex, "タブスコープの破棄中にエラーが発生しました: TabId={TabId}", tabState.TabId);
		}
	}

	/// <inheritdoc />
	protected override void Dispose(bool disposing) {
		if (disposing) {
			foreach (var kvp in this._tabViewModels) {
				try {
					kvp.Value.Dispose();
				} catch (Exception ex) {
					this._logger.LogError(ex, "TabViewModelの破棄中にエラーが発生しました: TabId={TabId}", kvp.Key);
				}
			}
			this._tabViewModels.Clear();

			foreach (var tabState in this._stateStore.RootState.Tabs) {
				try {
					(tabState.ServiceProvider as IDisposable)?.Dispose();
				} catch (Exception ex) {
					this._logger.LogError(ex, "タブスコープの破棄中にエラーが発生しました: TabId={TabId}", tabState.TabId);
				}
			}
		}
		base.Dispose(disposing);
	}
}