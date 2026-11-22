using MediaDeck.MediaItemTypes.Video.Services;
using Shouldly;

namespace MediaDeck.Core.Tests.Services;

/// <summary>
/// <see cref="MediaEngineInitializationCoordinator" /> のテストクラス。
/// </summary>
public class MediaEngineInitializationCoordinatorTest {
	/// <summary>
	/// 初回呼び出しで UI スレッド上からエンジンを起動し、Loaded 発火後に true を返して購読を解除することを確認する。
	/// </summary>
	[Fact]
	public async Task EnsureInitializedAsync_FirstCallOnUiThread_InitializesAndUnsubscribes() {
		var isLoaded = false;
		var startCount = 0;
		var subscribeCount = 0;
		var unsubscribeCount = 0;
		EventHandler? loadedHandler = null;

		var coordinator = new MediaEngineInitializationCoordinator(
			isLoaded: () => {
				return isLoaded;
			},
			subscribeLoaded: handler => {
				subscribeCount++;
				loadedHandler += handler;
			},
			unsubscribeLoaded: handler => {
				unsubscribeCount++;
				loadedHandler -= handler;
			},
			startEngineOnUiThread: () => {
				startCount++;
			},
			isOnUiThread: () => {
				return true;
			},
			dispatchToUiThread: _ => {
				throw new InvalidOperationException("UIスレッド上でディスパッチは呼ばれない想定です。");
			});

		var initTask = coordinator.EnsureInitializedAsync();

		startCount.ShouldBe(1);
		subscribeCount.ShouldBe(1);
		unsubscribeCount.ShouldBe(0);
		initTask.IsCompleted.ShouldBeFalse();

		isLoaded = true;
		loadedHandler?.Invoke(null, EventArgs.Empty);

		var result = await initTask;

		result.ShouldBeTrue();
		coordinator.IsInitialized.ShouldBeTrue();
		unsubscribeCount.ShouldBe(1);
		loadedHandler.ShouldBeNull();
	}

	/// <summary>
	/// 既にエンジンがロード済みの場合は起動処理を行わず即座に true を返すことを確認する。
	/// </summary>
	[Fact]
	public async Task EnsureInitializedAsync_WhenAlreadyLoaded_ReturnsTrueWithoutStarting() {
		var startCount = 0;
		var subscribeCount = 0;

		var coordinator = new MediaEngineInitializationCoordinator(
			isLoaded: () => {
				return true;
			},
			subscribeLoaded: _ => {
				subscribeCount++;
			},
			unsubscribeLoaded: _ => {
			},
			startEngineOnUiThread: () => {
				startCount++;
			},
			isOnUiThread: () => {
				return true;
			},
			dispatchToUiThread: _ => {
				return true;
			});

		var result = await coordinator.EnsureInitializedAsync();

		result.ShouldBeTrue();
		startCount.ShouldBe(0);
		subscribeCount.ShouldBe(0);
	}

	/// <summary>
	/// 複数の並行呼び出しに対して起動処理は 1 回だけ実行され、すべての呼び出し元に true が返ることを確認する。
	/// </summary>
	[Fact]
	public async Task EnsureInitializedAsync_ConcurrentCalls_StartsEngineOnceAndResolvesAll() {
		var isLoaded = false;
		var startCount = 0;
		var dispatchCount = 0;
		var startReady = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		EventHandler? loadedHandler = null;

		var coordinator = new MediaEngineInitializationCoordinator(
			isLoaded: () => {
				return isLoaded;
			},
			subscribeLoaded: handler => {
				loadedHandler += handler;
			},
			unsubscribeLoaded: handler => {
				loadedHandler -= handler;
			},
			startEngineOnUiThread: () => {
				Interlocked.Increment(ref startCount);
				startReady.TrySetResult();
			},
			isOnUiThread: () => {
				return false;
			},
			dispatchToUiThread: action => {
				Interlocked.Increment(ref dispatchCount);
				Task.Run(action);
				return true;
			});

		var tasks = Enumerable.Range(0, 8)
			.Select(_ => {
				return Task.Run(() => {
					return coordinator.EnsureInitializedAsync();
				});
			})
			.ToArray();

		await startReady.Task;

		startCount.ShouldBe(1);
		dispatchCount.ShouldBe(1);

		isLoaded = true;
		loadedHandler?.Invoke(null, EventArgs.Empty);

		var results = await Task.WhenAll(tasks);

		results.ShouldAllBe(r => r);
		startCount.ShouldBe(1);
	}

	/// <summary>
	/// 非 UI スレッドからの呼び出しが dispatchToUiThread を介して実行されることを確認する。
	/// </summary>
	[Fact]
	public async Task EnsureInitializedAsync_OffUiThread_DispatchesStartToUiThread() {
		var isLoaded = false;
		var dispatched = false;
		var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		EventHandler? loadedHandler = null;

		var coordinator = new MediaEngineInitializationCoordinator(
			isLoaded: () => {
				return isLoaded;
			},
			subscribeLoaded: handler => {
				loadedHandler += handler;
			},
			unsubscribeLoaded: handler => {
				loadedHandler -= handler;
			},
			startEngineOnUiThread: () => {
				started.TrySetResult();
			},
			isOnUiThread: () => {
				return false;
			},
			dispatchToUiThread: action => {
				dispatched = true;
				Task.Run(action);
				return true;
			});

		var initTask = coordinator.EnsureInitializedAsync();
		await started.Task;

		dispatched.ShouldBeTrue();

		isLoaded = true;
		loadedHandler?.Invoke(null, EventArgs.Empty);

		var result = await initTask;
		result.ShouldBeTrue();
	}

	/// <summary>
	/// UI スレッドへのディスパッチが失敗した場合に false を返し、次回呼び出しで再試行できることを確認する。
	/// </summary>
	[Fact]
	public async Task EnsureInitializedAsync_WhenDispatchFails_ReturnsFalseAndAllowsRetry() {
		var isLoaded = false;
		var canDispatch = false;
		var unsubscribeCount = 0;
		EventHandler? loadedHandler = null;

		var coordinator = new MediaEngineInitializationCoordinator(
			isLoaded: () => {
				return isLoaded;
			},
			subscribeLoaded: handler => {
				loadedHandler += handler;
			},
			unsubscribeLoaded: handler => {
				unsubscribeCount++;
				loadedHandler -= handler;
			},
			startEngineOnUiThread: () => {
				isLoaded = true;
				loadedHandler?.Invoke(null, EventArgs.Empty);
			},
			isOnUiThread: () => {
				return false;
			},
			dispatchToUiThread: action => {
				if (!canDispatch) {
					return false;
				}
				action();
				return true;
			});

		var firstResult = await coordinator.EnsureInitializedAsync();
		firstResult.ShouldBeFalse();
		unsubscribeCount.ShouldBe(1);

		canDispatch = true;
		var secondResult = await coordinator.EnsureInitializedAsync();
		secondResult.ShouldBeTrue();
	}

	/// <summary>
	/// startEngineOnUiThread 呼び出し直後に既に isLoaded が true の場合、Loaded イベントなしでも即座に true を返すことを確認する。
	/// </summary>
	[Fact]
	public async Task EnsureInitializedAsync_WhenLoadedSynchronouslyDuringStart_ReturnsTrueWithoutEvent() {
		var isLoaded = false;
		var unsubscribeCount = 0;

		var coordinator = new MediaEngineInitializationCoordinator(
			isLoaded: () => {
				return isLoaded;
			},
			subscribeLoaded: _ => {
			},
			unsubscribeLoaded: _ => {
				unsubscribeCount++;
			},
			startEngineOnUiThread: () => {
				isLoaded = true;
			},
			isOnUiThread: () => {
				return true;
			},
			dispatchToUiThread: _ => {
				return true;
			});

		var result = await coordinator.EnsureInitializedAsync();

		result.ShouldBeTrue();
		unsubscribeCount.ShouldBe(1);
	}

	/// <summary>
	/// 前提条件検証が false を返した場合はエンジンを起動せず false を返し、条件復旧後に再試行できることを確認する。
	/// </summary>
	[Fact]
	public async Task EnsureInitializedAsync_WhenPrerequisitesFail_ReturnsFalseWithoutStartingAndAllowsRetry() {
		var prerequisitesMet = false;
		var isLoaded = false;
		var startCount = 0;
		EventHandler? loadedHandler = null;

		var coordinator = new MediaEngineInitializationCoordinator(
			isLoaded: () => {
				return isLoaded;
			},
			subscribeLoaded: handler => {
				loadedHandler += handler;
			},
			unsubscribeLoaded: handler => {
				loadedHandler -= handler;
			},
			startEngineOnUiThread: () => {
				startCount++;
				isLoaded = true;
				loadedHandler?.Invoke(null, EventArgs.Empty);
			},
			isOnUiThread: () => {
				return true;
			},
			dispatchToUiThread: _ => {
				return true;
			},
			validatePrerequisites: () => {
				return prerequisitesMet;
			});

		var firstResult = await coordinator.EnsureInitializedAsync();
		firstResult.ShouldBeFalse();
		startCount.ShouldBe(0);

		prerequisitesMet = true;
		var secondResult = await coordinator.EnsureInitializedAsync();
		secondResult.ShouldBeTrue();
		startCount.ShouldBe(1);
	}

	/// <summary>
	/// 起動処理が同期例外をスローした場合にクラッシュせず false を返し、購読解除と再試行が可能であることを確認する。
	/// </summary>
	[Fact]
	public async Task EnsureInitializedAsync_WhenStartThrows_ReturnsFalseAndAllowsRetry() {
		var shouldThrow = true;
		var isLoaded = false;
		var unsubscribeCount = 0;
		EventHandler? loadedHandler = null;

		var coordinator = new MediaEngineInitializationCoordinator(
			isLoaded: () => {
				return isLoaded;
			},
			subscribeLoaded: handler => {
				loadedHandler += handler;
			},
			unsubscribeLoaded: handler => {
				unsubscribeCount++;
				loadedHandler -= handler;
			},
			startEngineOnUiThread: () => {
				if (shouldThrow) {
					throw new InvalidOperationException("Engine start failed.");
				}
				isLoaded = true;
				loadedHandler?.Invoke(null, EventArgs.Empty);
			},
			isOnUiThread: () => {
				return true;
			},
			dispatchToUiThread: _ => {
				return true;
			});

		var firstResult = await coordinator.EnsureInitializedAsync();
		firstResult.ShouldBeFalse();
		unsubscribeCount.ShouldBe(1);
		loadedHandler.ShouldBeNull();

		shouldThrow = false;
		var secondResult = await coordinator.EnsureInitializedAsync();
		secondResult.ShouldBeTrue();
		unsubscribeCount.ShouldBe(2);
	}

	/// <summary>
	/// Loaded イベントが発火しない場合にタイムアウトして false を返し、購読を解除することを確認する。
	/// </summary>
	[Fact]
	public async Task EnsureInitializedAsync_WhenLoadedDoesNotFire_TimesOutReturnsFalseAndUnsubscribes() {
		var unsubscribeCount = 0;
		EventHandler? loadedHandler = null;

		var coordinator = new MediaEngineInitializationCoordinator(
			isLoaded: () => {
				return false;
			},
			subscribeLoaded: handler => {
				loadedHandler += handler;
			},
			unsubscribeLoaded: handler => {
				unsubscribeCount++;
				loadedHandler -= handler;
			},
			startEngineOnUiThread: () => {
			},
			isOnUiThread: () => {
				return true;
			},
			dispatchToUiThread: _ => {
				return true;
			},
			timeout: TimeSpan.FromMilliseconds(80));

		var result = await coordinator.EnsureInitializedAsync();

		result.ShouldBeFalse();
		unsubscribeCount.ShouldBe(1);
		loadedHandler.ShouldBeNull();
	}

	/// <summary>
	/// 複数待機者のうち 1 つだけをキャンセルしても、他の待機者と初期化自体は正常完了することを確認する。
	/// </summary>
	[Fact]
	public async Task EnsureInitializedAsync_IndividualCallerCancelled_DoesNotCancelOtherWaiters() {
		var isLoaded = false;
		EventHandler? loadedHandler = null;

		var coordinator = new MediaEngineInitializationCoordinator(
			isLoaded: () => {
				return isLoaded;
			},
			subscribeLoaded: handler => {
				loadedHandler += handler;
			},
			unsubscribeLoaded: handler => {
				loadedHandler -= handler;
			},
			startEngineOnUiThread: () => {
			},
			isOnUiThread: () => {
				return true;
			},
			dispatchToUiThread: _ => {
				return true;
			});

		using var cts1 = new CancellationTokenSource();
		var task1 = coordinator.EnsureInitializedAsync(cts1.Token);
		var task2 = coordinator.EnsureInitializedAsync();

		cts1.Cancel();

		await Should.ThrowAsync<OperationCanceledException>(async () => {
			await task1;
		});

		task2.IsCompleted.ShouldBeFalse();

		isLoaded = true;
		loadedHandler?.Invoke(null, EventArgs.Empty);

		var result2 = await task2;
		result2.ShouldBeTrue();
	}

	/// <summary>
	/// 既にキャンセル済みのトークンが渡された場合、起動処理を実行せず OperationCanceledException をスローすることを確認する。
	/// </summary>
	[Fact]
	public async Task EnsureInitializedAsync_PreCancelledToken_ThrowsBeforeStarting() {
		var startCount = 0;

		var coordinator = new MediaEngineInitializationCoordinator(
			isLoaded: () => {
				return false;
			},
			subscribeLoaded: _ => {
			},
			unsubscribeLoaded: _ => {
			},
			startEngineOnUiThread: () => {
				startCount++;
			},
			isOnUiThread: () => {
				return true;
			},
			dispatchToUiThread: _ => {
				return true;
			});

		using var cts = new CancellationTokenSource();
		cts.Cancel();

		await Should.ThrowAsync<OperationCanceledException>(async () => {
			await coordinator.EnsureInitializedAsync(cts.Token);
		});

		startCount.ShouldBe(0);
	}
}