using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using FlyleafLib.MediaPlayer;
using MediaDeck.MediaItemTypes.UI.Video.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

namespace MediaDeck.MediaItemTypes.UI.Video.Controls;

/// <summary>
/// 動画再生を行う共通プレイヤーコントロール。
/// </summary>
public sealed partial class VideoPlayerControl : UserControl, INotifyPropertyChanged, IDisposable {
	private readonly SymbolIcon _iconPlay = new(Symbol.Play);
	private readonly SymbolIcon _iconPause = new(Symbol.Pause);
	private CancellationTokenSource? _loadCts;
	private Player? _ownedPlayer;
	private string? _pendingFilePath;
	private long _openRequestVersion;
	private bool _isUnloaded;
	private bool _isDisposed;

	/// <summary>
	/// プロパティ変更通知イベント。
	/// </summary>
	public event PropertyChangedEventHandler? PropertyChanged;

	/// <summary>
	/// プレイヤーの現在再生位置が変更されたときに発生します。
	/// </summary>
	public event EventHandler<TimeSpan>? CurTimeChanged;

	/// <summary>
	/// プレイヤープロパティの依存関係プロパティ。
	/// </summary>
	public static readonly DependencyProperty PlayerProperty =
		DependencyProperty.Register(
			nameof(Player),
			typeof(Player),
			typeof(VideoPlayerControl),
			new PropertyMetadata(null, (d, e) => ((VideoPlayerControl)d).OnPlayerChanged(e.OldValue as Player, e.NewValue as Player)));

	/// <summary>
	/// サムネイル画像ソースの依存関係プロパティ。
	/// </summary>
	public static readonly DependencyProperty ThumbnailSourceProperty =
		DependencyProperty.Register(
			nameof(ThumbnailSource),
			typeof(ImageSource),
			typeof(VideoPlayerControl),
			new PropertyMetadata(null, (d, e) => ((VideoPlayerControl)d).UpdateThumbnailVisibility()));

	/// <summary>
	/// サムネイル表示状態の依存関係プロパティ。
	/// </summary>
	public static readonly DependencyProperty ThumbnailVisibilityProperty =
		DependencyProperty.Register(
			nameof(ThumbnailVisibility),
			typeof(Visibility),
			typeof(VideoPlayerControl),
			new PropertyMetadata(Visibility.Collapsed));

	/// <summary>
	/// 再生プレイヤーを取得または設定します。
	/// </summary>
	public Player? Player {
		get {
			return (Player?)this.GetValue(PlayerProperty);
		}

		set {
			this.SetValue(PlayerProperty, value);
		}
	}

	/// <summary>
	/// 再生プレイヤーが利用可能かどうかを取得します。
	/// </summary>
	public bool IsPlayerReady {
		get {
			return this.Player != null;
		}
	}

	/// <summary>
	/// サムネイル画像ソースを取得または設定します。
	/// </summary>
	public ImageSource? ThumbnailSource {
		get {
			return (ImageSource?)this.GetValue(ThumbnailSourceProperty);
		}

		set {
			this.SetValue(ThumbnailSourceProperty, value);
		}
	}

	/// <summary>
	/// サムネイルの表示状態を取得または設定します。
	/// </summary>
	public Visibility ThumbnailVisibility {
		get {
			return (Visibility)this.GetValue(ThumbnailVisibilityProperty);
		}

		set {
			this.SetValue(ThumbnailVisibilityProperty, value);
		}
	}

	/// <summary>
	/// <see cref="VideoPlayerControl"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	public VideoPlayerControl() {
		this.InitializeComponent();
		this.rootGrid.DataContext = this;
		this.Loaded += this.OnLoaded;
		this.Unloaded += this.OnUnloaded;
		this.UpdateButtonState();

		this.sliderProgress.AddHandler(UIElement.PointerPressedEvent, new PointerEventHandler(this.OnSliderPointerAction), handledEventsToo: true);
		this.sliderProgress.AddHandler(UIElement.PointerWheelChangedEvent, new PointerEventHandler(this.OnSliderPointerAction), handledEventsToo: true);
		this.sliderProgress.AddHandler(UIElement.KeyDownEvent, new KeyEventHandler(this.OnSliderKeyAction), handledEventsToo: true);
	}

	/// <summary>
	/// 必要に応じてメディアエンジンを遅延初期化し、指定された動画ファイルを開いて一時停止状態で待機します。
	/// </summary>
	/// <param name="filePath">再生対象の動画ファイルパス。</param>
	public async Task OpenAsync(string filePath) {
		if (string.IsNullOrEmpty(filePath)) {
			this.Stop();
			return;
		}

		this._pendingFilePath = filePath;
		var requestVersion = ++this._openRequestVersion;

		if (this._isUnloaded || this._isDisposed) {
			return;
		}

		if (this.Player is { IsDisposed: false } existingPlayer) {
			existingPlayer.Open(filePath);
			existingPlayer.Pause();
			return;
		}

		var engineService = FlyleafEngineService.Current;
		if (engineService == null) {
			return;
		}

		var cts = this.ResetLoadCancellation();
		var token = cts.Token;
		bool initialized;
		try {
			initialized = await engineService.EnsureInitializedAsync(token);
		} catch (OperationCanceledException) {
			return;
		} catch (Exception) {
			return;
		} finally {
			if (ReferenceEquals(this._loadCts, cts)) {
				this._loadCts = null;
				cts.Dispose();
			}
		}

		if (!initialized || token.IsCancellationRequested || this._isUnloaded || this._isDisposed || this._openRequestVersion != requestVersion) {
			return;
		}

		var player = this.Player;
		if (player == null || player.IsDisposed) {
			this._ownedPlayer?.Dispose();
			this._ownedPlayer = new Player();
			this.Player = this._ownedPlayer;
			player = this._ownedPlayer;
		}

		player.Open(filePath);
		player.Pause();
	}

	/// <summary>
	/// 進行中の動画オープン要求をキャンセルし、現在の再生を停止します。
	/// </summary>
	public void Stop() {
		++this._openRequestVersion;
		this._pendingFilePath = null;
		this.CancelPendingLoad();
		this.Player?.Stop();
	}

	/// <summary>
	/// 現在の再生を一時停止します。
	/// </summary>
	public void Pause() {
		this.Player?.Pause();
	}

	/// <summary>
	/// 再生位置を指定された時刻へシークします。現在位置との差が 100ms 以下の場合は無視します。
	/// </summary>
	/// <param name="time">シーク先の時刻。</param>
	public void Seek(TimeSpan time) {
		var player = this.Player;
		if (player == null || player.IsDisposed) {
			return;
		}

		if (Math.Abs(player.CurTime - time.Ticks) > TimeSpan.FromMilliseconds(100).Ticks) {
			player.CurTime = time.Ticks;
		}
	}

	/// <summary>
	/// コントロールがロードされたときの初期化および保留中ファイルの再生開始を行います。
	/// </summary>
	/// <param name="sender">イベント発生元。</param>
	/// <param name="e">イベント引数。</param>
	private void OnLoaded(object sender, RoutedEventArgs e) {
		this._isUnloaded = false;
		if (this.Player != null && !this.Player.IsDisposed) {
			this.Player.PropertyChanged -= this.Player_PropertyChanged;
			this.Player.PropertyChanged += this.Player_PropertyChanged;
			this.UpdateButtonState();
		} else if (!string.IsNullOrEmpty(this._pendingFilePath)) {
			_ = this.OpenAsync(this._pendingFilePath);
		}
	}

	/// <summary>
	/// <see cref="Player"/> プロパティ変更時にイベント購読と表示状態を更新します。
	/// </summary>
	/// <param name="oldPlayer">変更前のプレイヤー。</param>
	/// <param name="newPlayer">変更後のプレイヤー。</param>
	private void OnPlayerChanged(Player? oldPlayer, Player? newPlayer) {
		oldPlayer?.PropertyChanged -= this.Player_PropertyChanged;

		if (newPlayer != null && !this._isUnloaded && !this._isDisposed) {
			newPlayer.PropertyChanged -= this.Player_PropertyChanged;
			newPlayer.PropertyChanged += this.Player_PropertyChanged;
		}

		this.RaisePropertyChanged(nameof(this.Player));
		this.RaisePropertyChanged(nameof(this.IsPlayerReady));
		this.UpdateButtonState();
		this.UpdateThumbnailVisibility();
	}

	/// <summary>
	/// プレイヤーのステータスおよび再生時刻の変更通知を処理します。
	/// </summary>
	/// <param name="sender">イベント発生元（<see cref="Player"/>）。</param>
	/// <param name="e">プロパティ変更イベント引数。</param>
	private void Player_PropertyChanged(object? sender, PropertyChangedEventArgs e) {
		if (sender is not Player player) {
			return;
		}

		switch (e.PropertyName) {
			case nameof(FlyleafLib.MediaPlayer.Player.Status):
				this.DispatcherQueue?.TryEnqueue(() => {
					this.UpdateButtonState();

					if (this.Player?.Status == Status.Playing) {
						this.ThumbnailVisibility = Visibility.Collapsed;
					}
				});
				break;

			case nameof(FlyleafLib.MediaPlayer.Player.CurTime):
				this.CurTimeChanged?.Invoke(this, TimeSpan.FromTicks(player.CurTime));
				break;
		}
	}

	/// <summary>
	/// 再生状態に応じて再生/一時停止ボタンのアイコンを更新します。
	/// </summary>
	private void UpdateButtonState() {
		if (this.btnPlayback == null) {
			return;
		}

		this.btnPlayback.Content = this.Player?.Status == Status.Playing ? this._iconPause : this._iconPlay;
	}

	/// <summary>
	/// サムネイル画像と再生状態に応じてサムネイルの表示状態を更新します。
	/// </summary>
	private void UpdateThumbnailVisibility() {
		if (this.ThumbnailSource != null && this.Player?.Status != Status.Playing) {
			this.ThumbnailVisibility = Visibility.Visible;
		} else {
			this.ThumbnailVisibility = Visibility.Collapsed;
		}
	}

	/// <summary>
	/// シークバーのポインター操作（クリック・ドラッグ・ホイール操作）時の処理。サムネイルを非表示にします。
	/// </summary>
	/// <param name="sender">イベント発生元。</param>
	/// <param name="e">ポインターイベント引数。</param>
	private void OnSliderPointerAction(object sender, PointerRoutedEventArgs e) {
		this.ThumbnailVisibility = Visibility.Collapsed;
	}

	/// <summary>
	/// シークバーのキー操作時の処理。サムネイルを非表示にします。
	/// </summary>
	/// <param name="sender">イベント発生元。</param>
	/// <param name="e">キーイベント引数。</param>
	private void OnSliderKeyAction(object sender, KeyRoutedEventArgs e) {
		this.ThumbnailVisibility = Visibility.Collapsed;
	}

	/// <summary>
	/// コントロールがアンロードされた際のクリーンアップを行います。
	/// </summary>
	/// <param name="sender">イベント発生元。</param>
	/// <param name="e">イベント引数。</param>
	private void OnUnloaded(object sender, RoutedEventArgs e) {
		this._isUnloaded = true;
		++this._openRequestVersion;
		this.CancelPendingLoad();
		this.ReleaseOwnedPlayer();
	}

	/// <summary>
	/// 待機中のエンジン初期化キャンセルトークンを更新して返します。
	/// </summary>
	/// <returns>新しいキャンセルトークンソース。</returns>
	private CancellationTokenSource ResetLoadCancellation() {
		this.CancelPendingLoad();
		var cts = new CancellationTokenSource();
		this._loadCts = cts;
		return cts;
	}

	/// <summary>
	/// 進行中のエンジン初期化待機をキャンセルして破棄します。
	/// </summary>
	private void CancelPendingLoad() {
		var cts = this._loadCts;
		this._loadCts = null;
		if (cts != null) {
			cts.Cancel();
			cts.Dispose();
		}
	}

	/// <summary>
	/// このコントロールが所有するプレイヤーを停止および破棄します。
	/// </summary>
	private void ReleaseOwnedPlayer() {
		var player = this._ownedPlayer;
		this._ownedPlayer = null;
		if (player != null) {
			player.PropertyChanged -= this.Player_PropertyChanged;
			player.Stop();
			player.Dispose();
			if (ReferenceEquals(this.Player, player)) {
				this.Player = null;
			}
		} else if (this.Player != null) {
			this.Player.PropertyChanged -= this.Player_PropertyChanged;
			this.Player.Pause();
		}
	}

	/// <summary>
	/// <see cref="PropertyChanged"/> イベントを発火します。
	/// </summary>
	/// <param name="propertyName">変更されたプロパティ名。</param>
	private void RaisePropertyChanged(string propertyName) {
		this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	/// <summary>
	/// リソースを破棄します。
	/// </summary>
	public void Dispose() {
		this._isDisposed = true;
		this._isUnloaded = true;
		++this._openRequestVersion;
		this._pendingFilePath = null;

		this._loadCts?.Cancel();
		this._loadCts?.Dispose();
		this._loadCts = null;

		this.Player?.PropertyChanged -= this.Player_PropertyChanged;

		this._ownedPlayer?.Stop();
		this._ownedPlayer?.Dispose();
		this._ownedPlayer = null;
		this.Player = null;
	}
}