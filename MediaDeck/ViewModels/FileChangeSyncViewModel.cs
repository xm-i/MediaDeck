using MediaDeck.Composition.Bases;
using MediaDeck.Models.Services;

namespace MediaDeck.ViewModels;

[Inject(InjectServiceLifetime.Transient)]
public class FileChangeSyncViewModel : ViewModelBase {
	private readonly FileChangeMonitorService _service;

	public INotifyCollectionChangedSynchronizedViewList<FileChangeItem> Changes {
		get;
	}

	public ReactiveCommand ApplyAllCommand {
		get;
	}
	public ReactiveCommand DiscardAllCommand {
		get;
	}
	public ReactiveCommand<FileChangeItem> ApplySingleCommand {
		get;
	}
	public ReactiveCommand<FileChangeItem> DiscardSingleCommand {
		get;
	}

	public FileChangeSyncViewModel(FileChangeMonitorService service) {
		this._service = service;
		this.Changes = this._service.UnprocessedChanges.ToNotifyCollectionChanged(SynchronizationContextCollectionEventDispatcher.Current);

		this.ApplyAllCommand = new ReactiveCommand();
		this.ApplyAllCommand.SubscribeAwait(async (_, ct) => {
			await this._service.ApplyChangesAsync(this.Changes.ToArray(), false);
		}).AddTo(this.CompositeDisposable);

		this.DiscardAllCommand = new ReactiveCommand();
		this.DiscardAllCommand.Subscribe(_ => {
			this._service.DiscardChanges(this.Changes.ToArray());
		}).AddTo(this.CompositeDisposable);

		this.ApplySingleCommand = new ReactiveCommand<FileChangeItem>();
		this.ApplySingleCommand.SubscribeAwait(async (item, ct) => {
			await this._service.ApplyChangesAsync([item], false);
		}).AddTo(this.CompositeDisposable);

		this.DiscardSingleCommand = new ReactiveCommand<FileChangeItem>();
		this.DiscardSingleCommand.Subscribe(item => {
			this._service.DiscardChanges([item]);
		}).AddTo(this.CompositeDisposable);
	}
}
