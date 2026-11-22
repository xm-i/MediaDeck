using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using MediaDeck.Common.Base;
using MediaDeck.Composition.Interfaces.Services;
using MediaDeck.Composition.Stores.Config.Model;
using MediaDeck.MediaItemTypes.Video.Services;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;

namespace MediaDeck.MediaItemTypes.UI.Video.Services;

/// <summary>
/// FlyleafLib メディアエンジンの初回利用時遅延初期化と終了時クリーンアップを管理するサービス。
/// 起動時に <c>FlyleafLib.dll</c> および依存アセンブリをロードしないよう、FlyleafLib へのアクセスを内部ネストクラスに隔離します。
/// </summary>
[Inject(InjectServiceLifetime.Singleton, typeof(IMediaEngineService))]
public sealed class FlyleafEngineService : ServiceBase, IMediaEngineService {
	private readonly PathConfigModel _pathConfig;
	private readonly ILogger<FlyleafEngineService> _logger;
	private readonly MediaEngineInitializationCoordinator _coordinator;
	private DispatcherQueue? _dispatcherQueue;
	private volatile bool _hasStarted;

	/// <summary>
	/// 現在のシングルトンサービスインスタンスを取得します。
	/// XAML の DataTemplate から生成されるコントロールがエンジン初期化を待機するために使用します。
	/// </summary>
	internal static IMediaEngineService? Current {
		get; private set;
	}

	/// <summary>
	/// <see cref="FlyleafEngineService"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	/// <param name="pathConfig">FFmpeg パス設定を保持するパス構成モデル。</param>
	/// <param name="logger">ロガー。</param>
	public FlyleafEngineService(PathConfigModel pathConfig, ILogger<FlyleafEngineService> logger) {
		this._pathConfig = pathConfig;
		this._logger = logger;
		this._dispatcherQueue = DispatcherQueue.GetForCurrentThread();
		this._coordinator = new MediaEngineInitializationCoordinator(
			isLoaded: () => {
				return this._hasStarted && FlyleafNativeBridge.IsLoaded();
			},
			subscribeLoaded: handler => {
				FlyleafNativeBridge.SubscribeLoaded(handler);
			},
			unsubscribeLoaded: handler => {
				FlyleafNativeBridge.UnsubscribeLoaded(handler);
			},
			startEngineOnUiThread: this.StartFlyleafEngineOnUiThread,
			isOnUiThread: this.IsOnUiThread,
			dispatchToUiThread: this.DispatchToUiThread,
			validatePrerequisites: this.ValidateFFmpegDirectory,
			logger: this._logger);

		Current = this;
	}

	/// <inheritdoc />
	public bool IsInitialized {
		get {
			if (!this._hasStarted) {
				return false;
			}
			return this._coordinator.IsInitialized;
		}
	}

	/// <inheritdoc />
	public Task<bool> EnsureInitializedAsync(CancellationToken cancellationToken = default) {
		return this._coordinator.EnsureInitializedAsync(cancellationToken);
	}

	/// <inheritdoc />
	public void Shutdown() {
		if (!this._hasStarted) {
			return;
		}

		var isOnUiThread = this._dispatcherQueue?.HasThreadAccess ?? false;
		FlyleafNativeBridge.ShutdownPlayers(detachHostBeforeDispose: !isOnUiThread, this._logger);
	}

	/// <summary>
	/// FFmpeg ライブラリ格納ディレクトリの存在を検証します。
	/// </summary>
	/// <returns>ディレクトリが有効な場合は true、無効または存在しない場合は false。</returns>
	private bool ValidateFFmpegDirectory() {
		var ffmpegPath = this._pathConfig.FFMpegFolderPath.Value;
		if (string.IsNullOrWhiteSpace(ffmpegPath) || !Directory.Exists(ffmpegPath)) {
			this._logger.LogError("FFmpeg ディレクトリが見つからないため、メディアエンジンを初期化できません。Path={FFmpegPath}", ffmpegPath);
			return false;
		}

		return true;
	}

	/// <summary>
	/// 現在のスレッドが UI スレッド（<see cref="DispatcherQueue"/> 所有スレッド）かどうかを判定します。
	/// </summary>
	/// <returns>UI スレッド上の場合は true、それ以外は false。</returns>
	private bool IsOnUiThread() {
		var queue = this._dispatcherQueue ??= DispatcherQueue.GetForCurrentThread();
		return queue?.HasThreadAccess ?? false;
	}

	/// <summary>
	/// 指定されたアクションを UI スレッドへディスパッチします。
	/// </summary>
	/// <param name="action">実行するアクション。</param>
	/// <returns>キューへの登録に成功した場合は true、失敗した場合は false。</returns>
	private bool DispatchToUiThread(Action action) {
		var queue = this._dispatcherQueue ??= DispatcherQueue.GetForCurrentThread();
		if (queue == null) {
			return false;
		}

		return queue.TryEnqueue(() => {
			action();
		});
	}

	/// <summary>
	/// UI スレッド上で FlyleafLib エンジンの非同期初期化を開始します。
	/// </summary>
	private void StartFlyleafEngineOnUiThread() {
		this._hasStarted = true;
		var ffmpegPath = this._pathConfig.FFMpegFolderPath.Value;
		FlyleafNativeBridge.StartAsync(ffmpegPath);
	}

	/// <inheritdoc />
	protected override void Dispose(bool disposing) {
		if (disposing && ReferenceEquals(Current, this)) {
			Current = null;
		}

		base.Dispose(disposing);
	}

	/// <summary>
	/// FlyleafLib への直接参照を分離するための静的ブリッジクラス。
	/// 初回利用時まで JIT コンパイルとアセンブリロードを遅延させるため、各メソッドに <see cref="MethodImplOptions.NoInlining"/> を付与します。
	/// </summary>
	private static class FlyleafNativeBridge {
		/// <summary>
		/// FlyleafLib エンジンがロード済みかどうかを取得します。
		/// </summary>
		/// <returns>ロード済みの場合は true。</returns>
		[MethodImpl(MethodImplOptions.NoInlining)]
		public static bool IsLoaded() {
			return FlyleafLib.Engine.IsLoaded;
		}

		/// <summary>
		/// FlyleafLib エンジンの <see cref="FlyleafLib.Engine.Loaded"/> イベントを購読します。
		/// </summary>
		/// <param name="handler">イベントハンドラ。</param>
		[MethodImpl(MethodImplOptions.NoInlining)]
		public static void SubscribeLoaded(EventHandler handler) {
			FlyleafLib.Engine.Loaded += handler;
		}

		/// <summary>
		/// FlyleafLib エンジンの <see cref="FlyleafLib.Engine.Loaded"/> イベントの購読を解除します。
		/// </summary>
		/// <param name="handler">イベントハンドラ。</param>
		[MethodImpl(MethodImplOptions.NoInlining)]
		public static void UnsubscribeLoaded(EventHandler handler) {
			FlyleafLib.Engine.Loaded -= handler;
		}

		/// <summary>
		/// FlyleafLib エンジンの非同期初期化を開始します。UI スレッド上から呼び出す必要があります。
		/// </summary>
		/// <param name="ffmpegPath">FFmpeg ライブラリのディレクトリパス。</param>
		[MethodImpl(MethodImplOptions.NoInlining)]
		public static void StartAsync(string ffmpegPath) {
			FlyleafLib.Engine.StartAsync(new FlyleafLib.EngineConfig() {
#if DEBUG
				LogOutput = ":debug",
				LogLevel = FlyleafLib.LogLevel.Debug,
				FFmpegLogLevel = Flyleaf.FFmpeg.LogLevel.Warning,
#endif
				UIRefresh = false,
				FFmpegPath = ffmpegPath,
				FFmpegLoadProfile = Flyleaf.FFmpeg.LoadProfile.Filters,
			});
		}

		/// <summary>
		/// 登録されているすべてのアクティブなプレイヤーを解放します。
		/// </summary>
		/// <param name="detachHostBeforeDispose">非 UI スレッドから終了する場合に <c>Player.Host</c> のデタッチを行うかどうか。</param>
		/// <param name="logger">ロガー。</param>
		[MethodImpl(MethodImplOptions.NoInlining)]
		public static void ShutdownPlayers(bool detachHostBeforeDispose, ILogger logger) {
			if (!FlyleafLib.Engine.IsLoaded) {
				return;
			}

			FlyleafLib.Engine.Config.UIRefresh = false;

			List<FlyleafLib.MediaPlayer.Player> players;
			lock (FlyleafLib.Engine.Players) {
				players = [.. FlyleafLib.Engine.Players];
			}

			foreach (var player in players) {
				try {
					if (detachHostBeforeDispose) {
						// ProcessExit 時は UI メッセージループが終了済みの可能性があり、Player_Disposed 内の UIInvokeIfRequired でハングするのを防ぐ
						player.Host = null;
					}

					player.Dispose();
				} catch (Exception ex) {
					logger.LogWarning(ex, "終了時のプレイヤー解放中にエラーが発生しました。PlayerId={PlayerId}", player.PlayerId);
				}
			}
		}
	}
}