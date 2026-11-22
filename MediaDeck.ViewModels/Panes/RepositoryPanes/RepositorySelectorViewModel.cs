using MediaDeck.Common.Base;
using MediaDeck.Common.Extensions;
using MediaDeck.Composition.Stores.State.Model;
using MediaDeck.Core.Models.Repositories;

namespace MediaDeck.ViewModels.Panes.RepositoryPanes;

/// <summary>
/// リポジトリセレクターペインの ViewModel。
/// </summary>
[Inject(InjectServiceLifetime.Scoped)]
public class RepositorySelectorViewModel : ViewModelBase {
	/// <summary>
	/// <see cref="RepositorySelectorViewModel"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	/// <param name="tabState">タブ状態モデル。</param>
	/// <param name="repositorySelector">リポジトリセレクターモデル。</param>
	/// <param name="folderRepositoryViewModel">フォルダリポジトリ ViewModel。</param>
	/// <param name="albumRepositoryViewModel">アルバムリポジトリ ViewModel。</param>
	public RepositorySelectorViewModel(
		TabStateModel tabState,
		RepositorySelector repositorySelector,
		FolderRepositoryViewModel folderRepositoryViewModel,
		AlbumRepositoryViewModel albumRepositoryViewModel) {
		this.RepositoryPaneViewModels = [folderRepositoryViewModel, albumRepositoryViewModel];
		this.FolderRepositoryViewModel = folderRepositoryViewModel;
		this.AlbumRepositoryViewModel = albumRepositoryViewModel;

		// Stateと双方向バインドするプロパティの作成
		this.SelectedRepositoryPane = tabState.ActiveRepository.ToTwoWayBindableReactiveProperty<RepositoryType, RepositoryViewModelBase>(
			type => type switch {
				RepositoryType.Folder => folderRepositoryViewModel,
				RepositoryType.Album => albumRepositoryViewModel,
				_ => folderRepositoryViewModel
			},
			vm => vm?.RepositoryType ?? RepositoryType.Folder,
			folderRepositoryViewModel,
			this.CompositeDisposable
		);

		// Model への反映
		this.SelectedRepositoryPane.Subscribe(vm => {
			if (vm is { } v) {
				repositorySelector.SelectedRepository.Value = v.Model;
			}
		}).AddTo(this.CompositeDisposable);

		this.LoadCommand = new ReactiveCommand().AddTo(this.CompositeDisposable);
		this.LoadCommand.SubscribeAwait(async (_, ct) => {
			try {
				await Task.WhenAll(repositorySelector.Repositories.Select(r => r.Load(ct))).ConfigureAwait(false);
			} catch (OperationCanceledException) when (ct.IsCancellationRequested) {
				// 破棄によるキャンセルは無視する
			}
		}, AwaitOperation.Drop, false).AddTo(this.CompositeDisposable);
	}

	/// <summary>
	/// 各リポジトリの読み込みを実行するコマンドを取得します。
	/// </summary>
	public ReactiveCommand LoadCommand {
		get;
	}

	/// <summary>
	/// 選択中のリポジトリペイン ViewModel を取得します。
	/// </summary>
	public BindableReactiveProperty<RepositoryViewModelBase> SelectedRepositoryPane {
		get;
	}

	/// <summary>
	/// 利用可能なリポジトリペイン ViewModel の配列を取得します。
	/// </summary>
	public RepositoryViewModelBase[] RepositoryPaneViewModels {
		get;
	}

	/// <summary>
	/// フォルダリポジトリの ViewModel を取得します。
	/// </summary>
	public FolderRepositoryViewModel FolderRepositoryViewModel {
		get;
	}

	/// <summary>
	/// アルバムリポジトリの ViewModel を取得します。
	/// </summary>
	public AlbumRepositoryViewModel AlbumRepositoryViewModel {
		get;
	}
}