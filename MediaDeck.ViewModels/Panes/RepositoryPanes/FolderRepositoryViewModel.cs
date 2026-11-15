using MediaDeck.Common.Extensions;
using MediaDeck.Composition.Stores.Config.Model;
using MediaDeck.Core.Models.Repositories;
using MediaDeck.Core.Models.Repositories.Objects;

namespace MediaDeck.ViewModels.Panes.RepositoryPanes;

/// <summary>
/// フォルダリポジトリペインの ViewModel。
/// </summary>
[Inject(InjectServiceLifetime.Scoped)]
public class FolderRepositoryViewModel : RepositoryViewModelBase {
	/// <summary>
	/// <see cref="FolderRepositoryViewModel"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	/// <param name="folderRepository">フォルダリポジトリモデル。</param>
	/// <param name="folderManagerConfig">フォルダ管理設定モデル。</param>
	public FolderRepositoryViewModel(FolderRepository folderRepository, FolderManagerConfigModel folderManagerConfig) : base("Folder", folderRepository) {
		this.RootFolder = folderRepository.RootFolder.ObserveOnCurrentSynchronizationContext().ToBindableReactiveProperty(null!).AddTo(this.CompositeDisposable);

		this.IsEmpty = folderRepository.RootFolder
			.Select(root => root is { ChildFolders.Length: 0 })
			.ObserveOnCurrentSynchronizationContext()
			.ToBindableReactiveProperty(folderRepository.RootFolder.Value is { ChildFolders.Length: 0 })
			.AddTo(this.CompositeDisposable);

		this.HasRegisteredFolders = folderManagerConfig.Folders
			.ObserveCountChanged()
			.Select(count => count > 0)
			.ObserveOnCurrentSynchronizationContext()
			.ToBindableReactiveProperty(folderManagerConfig.Folders.Count > 0)
			.AddTo(this.CompositeDisposable);

		this.ShowNoRegisteredFoldersGuide = this.IsEmpty
			.CombineLatest(this.HasRegisteredFolders, (isEmpty, hasFolders) => isEmpty && !hasFolders)
			.ObserveOnCurrentSynchronizationContext()
			.ToBindableReactiveProperty(this.IsEmpty.Value && !this.HasRegisteredFolders.Value)
			.AddTo(this.CompositeDisposable);

		this.ShowNoMediaItemsGuide = this.IsEmpty
			.CombineLatest(this.HasRegisteredFolders, (isEmpty, hasFolders) => isEmpty && hasFolders)
			.ObserveOnCurrentSynchronizationContext()
			.ToBindableReactiveProperty(this.IsEmpty.Value && this.HasRegisteredFolders.Value)
			.AddTo(this.CompositeDisposable);

		this.SetRepositoryConditionCommand.Merge(this.IncludeSubDirectories.ToUnit())
			.Subscribe(_ => {
				if (this.SelectedFolder.Value is not { } folder) {
					return;
				}
				folderRepository.SetRepositoryCandidate(folder, this.IncludeSubDirectories.Value);
			}).AddTo(this.CompositeDisposable);
	}

	/// <summary>
	/// ルートフォルダを取得します。
	/// </summary>
	public BindableReactiveProperty<FolderObject> RootFolder {
		get;
	}

	/// <summary>
	/// ロード完了後にフォルダツリーが空であるかどうかを取得します。
	/// </summary>
	public BindableReactiveProperty<bool> IsEmpty {
		get;
	}

	/// <summary>
	/// フォルダ管理設定に登録済みのフォルダが存在するかどうかを取得します。
	/// </summary>
	public BindableReactiveProperty<bool> HasRegisteredFolders {
		get;
	}

	/// <summary>
	/// フォルダ未登録時の誘導メッセージを表示するかどうかを取得します。
	/// </summary>
	public BindableReactiveProperty<bool> ShowNoRegisteredFoldersGuide {
		get;
	}

	/// <summary>
	/// フォルダ登録済みかつメディア未登録時の誘導メッセージを表示するかどうかを取得します。
	/// </summary>
	public BindableReactiveProperty<bool> ShowNoMediaItemsGuide {
		get;
	}

	/// <summary>
	/// 選択中のフォルダを取得します。
	/// </summary>
	public BindableReactiveProperty<FolderObject?> SelectedFolder {
		get;
	} = new();

	/// <summary>
	/// 選択フォルダをリポジトリ検索条件に設定するコマンドを取得します。
	/// </summary>
	public ReactiveCommand SetRepositoryConditionCommand {
		get;
	} = new();

	/// <summary>
	/// サブディレクトリを含めるかどうかを取得します。
	/// </summary>
	public BindableReactiveProperty<bool> IncludeSubDirectories {
		get;
	} = new(true);
}