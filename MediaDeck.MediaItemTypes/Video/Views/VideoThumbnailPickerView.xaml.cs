using FlyleafLib.MediaPlayer;
using MediaDeck.Composition.Interfaces.MediaItemTypes.Views;
using MediaDeck.MediaItemTypes.Base.Views;
using MediaDeck.MediaItemTypes.Video.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace MediaDeck.MediaItemTypes.Video.Views;

internal sealed partial class VideoThumbnailPickerView : VideoThumbnailPickerViewUserControl, IThumbnailPickerView {
	private readonly SymbolIcon _iconPlay = new(Symbol.Play);
	private readonly SymbolIcon _iconPause = new(Symbol.Pause);
	private readonly CompositeDisposable _disposables = [];

	public Player Player {
		get; set;
	}

	internal VideoThumbnailPickerView() {
		this.Player = new();
		this.InitializeComponent();
		this.btnPlayback.Content = this.Player.Status == Status.Paused ? this._iconPlay : this._iconPause;
		this.Player.PropertyChanged += this.Player_PropertyChanged;
		this.playerRootGrid.DataContext = this;

		this.Unloaded += (s, e) => {
			this._disposables.Dispose();
			this.Player.Dispose();
		};
	}

	protected override void OnViewModelChanged(VideoThumbnailPickerViewModel? oldViewModel, VideoThumbnailPickerViewModel? newViewModel) {
		base.OnViewModelChanged(oldViewModel, newViewModel);

		if (newViewModel == null) {
			return;
		}

		// ビデオファイルパスの変更を監視して再生
		newViewModel.VideoFilePath.Subscribe(filePath => {
			if (!string.IsNullOrEmpty(filePath)) {
				this.Player.Open(filePath);
			} else {
				this.Player.Stop();
			}
		}).AddTo(this._disposables);

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
		.AddTo(this._disposables);

		// ViewModel の Time 変更を Player に同期（シーク）
		newViewModel.Time.Subscribe(time => {
			if (Math.Abs(this.Player.CurTime - time.Ticks) > TimeSpan.FromMilliseconds(100).Ticks) {
				this.Player.CurTime = time.Ticks;
			}
		}).AddTo(this._disposables);
	}

	private void Player_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e) {
		switch (e.PropertyName) {
			case nameof(this.Player.Status):
				this.btnPlayback.Content = this.Player.Status == Status.Paused ? this._iconPlay : this._iconPause;
				break;
		}
	}
}

internal class VideoThumbnailPickerViewUserControl : UserControlBase<VideoThumbnailPickerViewModel> {
}