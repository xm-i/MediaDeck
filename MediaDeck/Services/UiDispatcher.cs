using System.Threading.Tasks;
using MediaDeck.Composition.Interfaces.Threading;
using Microsoft.UI.Dispatching;

namespace MediaDeck.Services;

/// <summary>
/// Windows App SDK の DispatcherQueue を利用した UI スレッド同期ディスパッチャーの実装。
/// </summary>
[Inject(InjectServiceLifetime.Singleton, typeof(IUiDispatcher))]
public class UiDispatcher : IUiDispatcher {
	private readonly DispatcherQueue? _dispatcherQueue;

	/// <summary>
	/// コンストラクタ。
	/// </summary>
	public UiDispatcher() {
		this._dispatcherQueue = DispatcherQueue.GetForCurrentThread();
	}

	/// <inheritdoc />
	public void Run(Action action) {
		if (this._dispatcherQueue == null || this._dispatcherQueue.HasThreadAccess) {
			action();
			return;
		}

		var tcs = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
		var success = this._dispatcherQueue.TryEnqueue(() => {
			try {
				action();
				tcs.SetResult(null);
			} catch (Exception ex) {
				tcs.SetException(ex);
			}
		});

		if (success) {
			tcs.Task.GetAwaiter().GetResult();
		} else {
			throw new InvalidOperationException("Failed to enqueue action to the DispatcherQueue.");
		}
	}

	/// <inheritdoc />
	public async Task RunAsync(Action action) {
		if (this._dispatcherQueue == null || this._dispatcherQueue.HasThreadAccess) {
			action();
			return;
		}

		var tcs = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
		var success = this._dispatcherQueue.TryEnqueue(() => {
			try {
				action();
				tcs.SetResult(null);
			} catch (Exception ex) {
				tcs.SetException(ex);
			}
		});

		if (!success) {
			throw new InvalidOperationException("Failed to enqueue action to the DispatcherQueue.");
		}

		await tcs.Task.ConfigureAwait(false);
	}

	/// <inheritdoc />
	public async Task RunAsync(Func<Task> action) {
		if (this._dispatcherQueue == null || this._dispatcherQueue.HasThreadAccess) {
			await action().ConfigureAwait(false);
			return;
		}

		var tcs = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
		var success = this._dispatcherQueue.TryEnqueue(async () => {
			try {
				await action().ConfigureAwait(false);
				tcs.SetResult(null);
			} catch (Exception ex) {
				tcs.SetException(ex);
			}
		});

		if (!success) {
			throw new InvalidOperationException("Failed to enqueue action to the DispatcherQueue.");
		}

		await tcs.Task.ConfigureAwait(false);
	}
}