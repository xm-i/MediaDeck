using MediaDeck.Common.Base;
using MediaDeck.Composition.Stores.State.Model;
using MediaDeck.ViewModels.Panes.DetailPanes;
using MediaDeck.ViewModels.Panes.FilterPanes;
using MediaDeck.ViewModels.Panes.RepositoryPanes;
using MediaDeck.ViewModels.Panes.ViewerPanes;

using Microsoft.Extensions.DependencyInjection;

namespace MediaDeck.ViewModels;

/// <summary>
/// 1つのタブに対応するDIスコープとViewModel群を管理するコンテキスト。
/// Disposeするとスコープが破棄され、スコープ内の全サービスが解放される。
/// </summary>
public class TabContext : ViewModelBase {
	private readonly IDisposable? _scopeDisposable;

	/// <summary>
	/// このタブの状態モデル
	/// </summary>
	public TabStateModel TabState {
		get;
	}

	/// <summary>
	/// タブの表示名
	/// </summary>
	public BindableReactiveProperty<string> DisplayName {
		get;
	}

	public ViewerSelectorViewModel ViewerSelectorViewModel {
		get;
	}

	public FilterSelectorViewModel FilterSelectorViewModel {
		get;
	}

	public DetailSelectorViewModel DetailSelectorViewModel {
		get;
	}

	public RepositorySelectorViewModel RepositorySelectorViewModel {
		get;
	}

	public StatusBarViewModel StatusBarViewModel {
		get;
	}

	public TabContext(TabStateModel tabState) {
		this.TabState = tabState;
		var sp = tabState.ServiceProvider;
		this._scopeDisposable = sp as IDisposable;

		this.DisplayName = this.TabState.DisplayName.ToBindableReactiveProperty("New Tab").AddTo(this.CompositeDisposable);

		this.ViewerSelectorViewModel = sp.GetRequiredService<ViewerSelectorViewModel>();
		this.FilterSelectorViewModel = sp.GetRequiredService<FilterSelectorViewModel>();
		this.DetailSelectorViewModel = sp.GetRequiredService<DetailSelectorViewModel>();
		this.RepositorySelectorViewModel = sp.GetRequiredService<RepositorySelectorViewModel>();
		this.StatusBarViewModel = sp.GetRequiredService<StatusBarViewModel>().AddTo(this.CompositeDisposable);

		this.ViewerSelectorViewModel.MediaContentLibraryViewModel.SelectedFiles.Subscribe(x => {
			this.DetailSelectorViewModel.TargetFiles.Value = x.Select(v => v.FileModel).ToArray();
		}).AddTo(this.CompositeDisposable);

		// 初回ロード
		this.ViewerSelectorViewModel.MediaContentLibraryViewModel.Reload();
	}

	protected override void Dispose(bool disposing) {
		if (disposing) {
			this._scopeDisposable?.Dispose();
		}
		base.Dispose(disposing);
	}
}