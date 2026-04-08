namespace MediaDeck.Composition.Interfaces.Services;

public interface IUpdateFileHashBackgroundService : IServiceBase {
	public ReactiveProperty<long> CompletedCount {
		get;
	}

	public ReactiveProperty<long> FullHashCompletedCount {
		get;
	}

	public ReactiveProperty<long> FullHashTargetCount {
		get;
	}

	public ObservableQueue<long> FullHashUpdateQueue {
		get;
	}

	public ObservableQueue<long> HashUpdateQueue {
		get;
	}

	public ReactiveProperty<long> TargetCount {
		get;
	}

	public Task CheckAndEnqueueFullHashUpdatesAsync(CancellationToken ct = default);
	public void EnqueueHashUpdate(long mediaFileId);
	public void EnqueueHashUpdateRange(IEnumerable<long> mediaFileIds);
}