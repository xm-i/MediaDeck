using FlyleafLib.MediaPlayer;
using MediaDeck.MediaItemTypes.UI.Base.Views;
using MediaDeck.MediaItemTypes.Video.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace MediaDeck.MediaItemTypes.UI.Video.Views;

/// <summary>
/// ビデオサムネイル選択ビュー。
/// </summary>
public sealed partial class VideoThumbnailPickerView : VideoThumbnailPickerViewUserControl {
	private readonly SymbolIcon _iconPlay = new(Symbol.Play);
	private readonly SymbolIcon _iconPause = new(Symbol.Pause);
	private readonly SerialDisposable _viewModelDisposable = new();

	public Player Player {
		get; set;
	}

	public VideoThumbnailPickerView() {
		this.Player = new();
		this.InitializeComponent();
		this.btnPlayback.Content = this.Player.Status == Status.Playing ? this._iconPause : this._iconPlay;
		this.Player.PropertyChanged += this.Player_PropertyChanged;
		this.playerRootGrid.DataContext = this;
		this.Unloaded += this.VideoThumbnailPickerView_Unloaded;
	}

	/// <summary>
	/// ViewModelが変更された際の処理。古い購読を解除し新しいViewModelとの同期を設定します。
	/// </summary>
	/// <param name="oldViewModel">古いViewModel。</param>
	/// <param name="newViewModel">新しいViewModel。</param>
	protected override void OnViewModelChanged(VideoThumbnailPickerViewModel? oldViewModel, VideoThumbnailPickerViewModel? newViewModel) {
		base.OnViewModelChanged(oldViewModel, newViewModel);

		if (newViewModel == null) {
			this._viewModelDisposable.Disposable = null;
			this.Player.Stop();
			return;
		}

		var disposables = new CompositeDisposable();
		this._viewModelDisposable.Disposable = disposables;

		// ビデオファイルパスの変更を監視して再生
		newViewModel.VideoFilePath.Subscribe(filePath => {
			if (!string.IsNullOrEmpty(filePath)) {
				this.Player.Open(filePath);
				this.Player.Pause();
			} else {
				this.Player.Stop();
			}
		}).AddTo(disposables);

		// Player の位置変更を ViewModel に同期
		Observable.FromEvent<System.ComponentModel.PropertyChangedEventHandler, System.ComponentModel.PropertyChangedEventArgs>(
			h => (s, e) => h(e),
			h => this.Player.PropertyChanged += h,
			h => this.Player.PropertyChanged -= h
		)
		.Where(e => e.PropertyName == nameof(this.Player.CurTime))
		.Subscribe(_ => {
			newViewModel.UpdateTime(TimeSpan.FromTicks(this.Player.CurTime));
		})
		.AddTo(disposables);

		// ViewModel の Time 変更を Player に同期（シーク）
		newViewModel.Time.Subscribe(time => {
			if (Math.Abs(this.Player.CurTime - time.Ticks) > TimeSpan.FromMilliseconds(100).Ticks) {
				this.Player.CurTime = time.Ticks;
			}
		}).AddTo(disposables);
	}

	/// <summary>
	/// プレイヤーのプロパティ変更イベントハンドラ。
	/// </summary>
	/// <param name="sender">イベント発生元。</param>
	/// <param name="e">イベント引数。</param>
	private void Player_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e) {
		switch (e.PropertyName) {
			case nameof(this.Player.Status):
				this.btnPlayback.Content = this.Player.Status == Status.Playing ? this._iconPause : this._iconPlay;
				break;
		}
	}

	/// <summary>
	/// ビューがアンロードされた際に呼び出され、再生を一時停止します。
	/// </summary>
	/// <param name="sender">イベントの発生元。</param>
	/// <param name="e">イベントデータ。</param>
	private void VideoThumbnailPickerView_Unloaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e) {
		this.Player.Pause();
	}
}

public class VideoThumbnailPickerViewUserControl : UserControlBase<VideoThumbnailPickerViewModel> {
}