using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace MediaDeck.MediaItemTypes.Video.Services;

/// <summary>
/// メディア再生エンジンの遅延初期化におけるスレッド同期・重複起動防止・待機・リトライ制御を行うコーディネーター。
/// </summary>
public sealed class MediaEngineInitializationCoordinator {
	private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);

	private readonly Func<bool> _isLoaded;
	private readonly Action<EventHandler> _subscribeLoaded;
	private readonly Action<EventHandler> _unsubscribeLoaded;
	private readonly Action _startEngineOnUiThread;
	private readonly Func<bool> _isOnUiThread;
	private readonly Func<Action, bool> _dispatchToUiThread;
	private readonly Func<bool>? _validatePrerequisites;
	private readonly TimeSpan _timeout;
	private readonly ILogger? _logger;
	private readonly Lock _syncLock = new();
	private Task<bool>? _initializationTask;

	/// <summary>
	/// <see cref="MediaEngineInitializationCoordinator"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	/// <param name="isLoaded">エンジンが初期化済みかどうかを返すデリゲート。</param>
	/// <param name="subscribeLoaded">初期化完了イベントを購読するデリゲート。</param>
	/// <param name="unsubscribeLoaded">初期化完了イベントの購読を解除するデリゲート。</param>
	/// <param name="startEngineOnUiThread">UI スレッド上でエンジン起動を開始するデリゲート。</param>
	/// <param name="isOnUiThread">現在のスレッドが UI スレッドかどうかを判定するデリゲート。</param>
	/// <param name="dispatchToUiThread">UI スレッドへ処理をディスパッチするデリゲート。</param>
	/// <param name="validatePrerequisites">起動前の前提条件を検証するデリゲート（省略可）。</param>
	/// <param name="timeout">初期化完了待機のタイムアウト時間（省略時は 30 秒）。</param>
	/// <param name="logger">ロガー（省略可）。</param>
	public MediaEngineInitializationCoordinator(
		Func<bool> isLoaded,
		Action<EventHandler> subscribeLoaded,
		Action<EventHandler> unsubscribeLoaded,
		Action startEngineOnUiThread,
		Func<bool> isOnUiThread,
		Func<Action, bool> dispatchToUiThread,
		Func<bool>? validatePrerequisites = null,
		TimeSpan? timeout = null,
		ILogger? logger = null) {
		ArgumentNullException.ThrowIfNull(isLoaded);
		ArgumentNullException.ThrowIfNull(subscribeLoaded);
		ArgumentNullException.ThrowIfNull(unsubscribeLoaded);
		ArgumentNullException.ThrowIfNull(startEngineOnUiThread);
		ArgumentNullException.ThrowIfNull(isOnUiThread);
		ArgumentNullException.ThrowIfNull(dispatchToUiThread);

		this._isLoaded = isLoaded;
		this._subscribeLoaded = subscribeLoaded;
		this._unsubscribeLoaded = unsubscribeLoaded;
		this._startEngineOnUiThread = startEngineOnUiThread;
		this._isOnUiThread = isOnUiThread;
		this._dispatchToUiThread = dispatchToUiThread;
		this._validatePrerequisites = validatePrerequisites;
		this._timeout = timeout ?? DefaultTimeout;
		this._logger = logger;
	}

	/// <summary>
	/// メディアエンジンが初期化済みかどうかを取得します。
	/// </summary>
	public bool IsInitialized {
		get {
			return this._isLoaded();
		}
	}

	/// <summary>
	/// メディアエンジンの初期化が完了していることを保証します。未初期化の場合は UI スレッド上で初期化を開始し、完了まで待機します。
	/// </summary>
	/// <param name="cancellationToken">呼び出し元個別の待機をキャンセルするためのキャンセルトークン。</param>
	/// <returns>初期化が成功した場合は true、失敗または前提条件不備の場合は false。</returns>
	public Task<bool> EnsureInitializedAsync(CancellationToken cancellationToken = default) {
		cancellationToken.ThrowIfCancellationRequested();

		if (this._isLoaded()) {
			return Task.FromResult(true);
		}

		Task<bool> sharedTask;
		TaskCompletionSource<bool>? newSharedTcs = null;

		lock (this._syncLock) {
			if (this._isLoaded()) {
				return Task.FromResult(true);
			}

			if (this._initializationTask is { } existingTask) {
				sharedTask = existingTask;
			} else {
				if (this._validatePrerequisites != null && !this._validatePrerequisites()) {
					return Task.FromResult(false);
				}

				newSharedTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
				sharedTask = newSharedTcs.Task;
				this._initializationTask = sharedTask;
			}
		}

		if (newSharedTcs != null) {
			_ = this.RunInitializationAsync(newSharedTcs);
		}

		return WaitForInitializationAsync(sharedTask, cancellationToken);
	}

	/// <summary>
	/// 共有初期化タスクの完了を呼び出し元個別のキャンセルトークン付きで待機します。
	/// </summary>
	/// <param name="task">共有初期化タスク。</param>
	/// <param name="cancellationToken">呼び出し元の待機をキャンセルするトークン。</param>
	/// <returns>初期化が成功した場合は true、失敗した場合は false。</returns>
	private static async Task<bool> WaitForInitializationAsync(Task<bool> task, CancellationToken cancellationToken) {
		if (cancellationToken.CanBeCanceled) {
			return await task.WaitAsync(cancellationToken).ConfigureAwait(false);
		}

		return await task.ConfigureAwait(false);
	}

	/// <summary>
	/// エンジンの起動イベント購読、UI スレッドでの起動開始、完了またはタイムアウトの待機を実行します。
	/// </summary>
	/// <param name="sharedTcs">共有結果を通知する完了ソース。</param>
	private async Task RunInitializationAsync(TaskCompletionSource<bool> sharedTcs) {
		var loadedTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		EventHandler loadedHandler = (_, _) => {
			loadedTcs.TrySetResult(true);
		};
		var subscribed = false;
		var result = false;

		try {
			// イベント取りこぼしを防ぐため、起動開始前に Loaded イベントを購読する
			this._subscribeLoaded(loadedHandler);
			subscribed = true;

			if (this._isLoaded()) {
				result = true;
				return;
			}

			void InvokeStart() {
				try {
					this._startEngineOnUiThread();
					if (this._isLoaded()) {
						loadedTcs.TrySetResult(true);
					}
				} catch (Exception ex) {
					this._logger?.LogError(ex, "メディアエンジンの起動開始中に例外が発生しました。");
					loadedTcs.TrySetResult(false);
				}
			}

			if (this._isOnUiThread()) {
				InvokeStart();
			} else if (!this._dispatchToUiThread(InvokeStart)) {
				this._logger?.LogError("メディアエンジン初期化の UI スレッドへのディスパッチに失敗しました。");
				loadedTcs.TrySetResult(false);
			}

			using var timeoutCts = new CancellationTokenSource(this._timeout);
			try {
				result = await loadedTcs.Task.WaitAsync(timeoutCts.Token).ConfigureAwait(false);
			} catch (OperationCanceledException) {
				result = this._isLoaded();
				if (!result) {
					this._logger?.LogError("メディアエンジンの初期化がタイムアウトしました ({TimeoutSeconds} 秒)。", this._timeout.TotalSeconds);
				}
			}
		} catch (Exception ex) {
			this._logger?.LogError(ex, "メディアエンジンの初期化処理中に予期しない例外が発生しました。");
			result = this._isLoaded();
		} finally {
			if (subscribed) {
				try {
					this._unsubscribeLoaded(loadedHandler);
				} catch (Exception ex) {
					this._logger?.LogWarning(ex, "メディアエンジン Loaded イベントの購読解除中に例外が発生しました。");
				}
			}

			if (!result) {
				// 失敗時のみ次回呼び出しで再試行できるよう進行中タスク参照をクリアする
				lock (this._syncLock) {
					if (ReferenceEquals(this._initializationTask, sharedTcs.Task)) {
						this._initializationTask = null;
					}
				}
			}

			sharedTcs.TrySetResult(result);
		}
	}
}