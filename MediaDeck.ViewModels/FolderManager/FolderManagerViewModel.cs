using MediaDeck.Common.Base;
using MediaDeck.Core.Models.FolderManager;

namespace MediaDeck.ViewModels.FolderManager;

/// <summary>
/// フォルダー管理画面のViewModelを表します。
/// </summary>
[Inject(InjectServiceLifetime.Transient)]
public class FolderManagerViewModel : ViewModelBase {
	private readonly FolderManagerModel _folderManager;

	/// <summary>
	/// 登録フォルダー項目のリストを取得します。
	/// </summary>
	public INotifyCollectionChangedSynchronizedViewList<FolderViewModel> Folders {
		get;
	}

	/// <summary>
	/// 現在選択されているフォルダー項目を取得または設定します。
	/// </summary>
	public BindableReactiveProperty<FolderViewModel?> SelectedFolder {
		get;
	}

	/// <summary>
	/// 登録フォルダ一覧が空であるかどうかを取得します。
	/// </summary>
	public BindableReactiveProperty<bool> IsEmpty {
		get;
	}

	/// <summary>
	/// フォルダ未選択かつ登録フォルダ数が0のときに、フォルダ登録を促すメッセージを表示するかどうかを取得します。
	/// </summary>
	public BindableReactiveProperty<bool> ShowEmptyMessage {
		get;
	}

	/// <summary>
	/// フォルダ未選択かつ登録フォルダ数が1以上のときに、フォルダ選択を促すメッセージを表示するかどうかを取得します。
	/// </summary>
	public BindableReactiveProperty<bool> ShowSelectFolderMessage {
		get;
	}

	/// <summary>
	/// フォルダーを追加するコマンドを取得します。
	/// </summary>
	public ReactiveCommand<string> AddFolderCommand {
		get;
	}

	/// <summary>
	/// フォルダーを削除するコマンドを取得します。
	/// </summary>
	public ReactiveCommand<FolderViewModel> RemoveFolderCommand {
		get;
	}

	/// <summary>
	/// すべてのフォルダーをスキャンするコマンドを取得します。
	/// </summary>
	public ReactiveCommand ScanCommand {
		get;
	}

	/// <summary>
	/// 選択されたフォルダーをスキャンするコマンドを取得します。
	/// </summary>
	public ReactiveCommand<FolderViewModel> ScanSelectedFolderCommand {
		get;
	}

	/// <summary>
	/// <see cref="FolderManagerViewModel"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	/// <param name="folderManager">フォルダー管理モデル</param>
	public FolderManagerViewModel(FolderManagerModel folderManager) {
		this._folderManager = folderManager;

		this.SelectedFolder = new BindableReactiveProperty<FolderViewModel?>().AddTo(this.CompositeDisposable);
		this.AddFolderCommand = new ReactiveCommand<string>().AddTo(this.CompositeDisposable);
		this.RemoveFolderCommand = new ReactiveCommand<FolderViewModel>().AddTo(this.CompositeDisposable);
		this.ScanCommand = new ReactiveCommand().AddTo(this.CompositeDisposable);
		this.ScanSelectedFolderCommand = new ReactiveCommand<FolderViewModel>().AddTo(this.CompositeDisposable);

		this.Folders = this._folderManager.Folders
			.CreateView(x => new FolderViewModel(x))
			.ToNotifyCollectionChanged(SynchronizationContextCollectionEventDispatcher.Current);
		this.Folders.AddTo(this.CompositeDisposable);

		this.IsEmpty = this._folderManager.Folders
			.ObserveCountChanged()
			.Select(count => count == 0)
			.ToBindableReactiveProperty(this._folderManager.Folders.Count == 0)
			.AddTo(this.CompositeDisposable);

		this.ShowEmptyMessage = this.SelectedFolder
			.CombineLatest(this.IsEmpty, (selected, isEmpty) => selected is null && isEmpty)
			.ObserveOnCurrentSynchronizationContext()
			.ToBindableReactiveProperty(this.SelectedFolder.Value is null && this.IsEmpty.Value)
			.AddTo(this.CompositeDisposable);

		this.ShowSelectFolderMessage = this.SelectedFolder
			.CombineLatest(this.IsEmpty, (selected, isEmpty) => selected is null && !isEmpty)
			.ObserveOnCurrentSynchronizationContext()
			.ToBindableReactiveProperty(this.SelectedFolder.Value is null && !this.IsEmpty.Value)
			.AddTo(this.CompositeDisposable);

		this.AddFolderCommand.Subscribe(x => this._folderManager.AddFolder(x)).AddTo(this.CompositeDisposable);
		this.RemoveFolderCommand.Subscribe(x => this._folderManager.RemoveFolder(x.GetModel())).AddTo(this.CompositeDisposable);
		this.ScanCommand.SubscribeAwait(async (x, ct) => await this._folderManager.Scan(), AwaitOperation.Drop).AddTo(this.CompositeDisposable);
		this.ScanSelectedFolderCommand.SubscribeAwait(async (x, ct) => {
			if (x is not null) {
				await this._folderManager.ScanFolder(x.GetModel());
			}
		}, AwaitOperation.Drop)
			.AddTo(this.CompositeDisposable);
	}
}