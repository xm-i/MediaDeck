using FlyleafLib.MediaPlayer;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

namespace MediaDeck.MediaItemTypes.UI.Video.Controls;

/// <summary>
/// 動画再生を行う共通プレイヤーコントロール。
/// </summary>
public sealed partial class VideoPlayerControl : UserControl, IDisposable {
	private readonly CompositeDisposable _compositeDisposable = new();
	private readonly SymbolIcon _iconPlay = new(Symbol.Play);
	private readonly SymbolIcon _iconPause = new(Symbol.Pause);

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
	public Player Player {
		get {
			return (Player)this.GetValue(PlayerProperty);
		}

		set {
			this.SetValue(PlayerProperty, value);
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
		this.Player = new Player().AddTo(this._compositeDisposable);
		this.rootGrid.DataContext = this;
		this.Loaded += this.OnLoaded;
		this.Unloaded += this.OnUnloaded;
		this.UpdateButtonState();

		this.sliderProgress.AddHandler(UIElement.PointerPressedEvent, new PointerEventHandler(this.OnSliderPointerAction), handledEventsToo: true);
		this.sliderProgress.AddHandler(UIElement.PointerWheelChangedEvent, new PointerEventHandler(this.OnSliderPointerAction), handledEventsToo: true);
		this.sliderProgress.AddHandler(UIElement.KeyDownEvent, new KeyEventHandler(this.OnSliderKeyAction), handledEventsToo: true);
	}

	private void OnLoaded(object sender, RoutedEventArgs e) {
		if (this.Player != null) {
			this.Player.PropertyChanged -= this.Player_PropertyChanged;
			this.Player.PropertyChanged += this.Player_PropertyChanged;
			this.UpdateButtonState();
		}
	}

	private void OnPlayerChanged(Player? oldPlayer, Player? newPlayer) {
		oldPlayer?.PropertyChanged -= this.Player_PropertyChanged;

		newPlayer?.PropertyChanged += this.Player_PropertyChanged;

		this.UpdateButtonState();
		this.UpdateThumbnailVisibility();
	}

	private void Player_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e) {
		switch (e.PropertyName) {
			case nameof(this.Player.Status):
				this.DispatcherQueue?.TryEnqueue(() => {
					this.UpdateButtonState();

					if (this.Player?.Status == Status.Playing) {
						this.ThumbnailVisibility = Visibility.Collapsed;
					}
				});
				break;
		}
	}

	private void UpdateButtonState() {
		if (this.btnPlayback == null || this.Player == null) {
			return;
		}
		this.btnPlayback.Content = this.Player.Status == Status.Playing ? this._iconPause : this._iconPlay;
	}

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

	private void OnUnloaded(object sender, RoutedEventArgs e) {
		if (this.Player != null) {
			this.Player.PropertyChanged -= this.Player_PropertyChanged;
			this.Player.Pause();
		}
	}

	/// <summary>
	/// リソースを破棄します。
	/// </summary>
	public void Dispose() {
		this.Player?.PropertyChanged -= this.Player_PropertyChanged;
		this._compositeDisposable.Dispose();
	}
}