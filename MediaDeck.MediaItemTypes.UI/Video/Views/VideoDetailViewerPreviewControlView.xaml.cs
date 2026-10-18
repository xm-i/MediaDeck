using FlyleafLib.MediaPlayer;

using MediaDeck.Composition.Interfaces.MediaItemTypes.ViewModels;
using MediaDeck.MediaItemTypes.UI.Base.Views;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace MediaDeck.MediaItemTypes.UI.Video.Views;

/// <summary>
/// ビデオプレビュー詳細ビューのコントロール。
/// </summary>
public sealed partial class VideoDetailViewerPreviewControlView : VideoDetailViewerPreviewControlViewUserControl {
	private readonly SymbolIcon _iconPlay = new(Symbol.Play);
	private readonly SymbolIcon _iconPause = new(Symbol.Pause);
	public Player Player {
		get; set;
	}

	/// <summary>
	/// サムネイルの表示状態。
	/// </summary>
	public BindableReactiveProperty<Visibility> ThumbnailVisibility { get; } = new(Visibility.Collapsed);

	public VideoDetailViewerPreviewControlView() {
		this.Player = new();

		this.InitializeComponent();
		this.btnPlayback.Content = this.Player.Status == Status.Playing ? this._iconPause : this._iconPlay;
		this.Player.PropertyChanged += this.Player_PropertyChanged;
		this.rootGrid.DataContext = this;
		this.Unloaded += this.OnUnloaded;
	}

	/// <summary>
	/// ViewModelが変更された際の処理。ビデオを停止または新規オープンします。
	/// </summary>
	/// <param name="oldViewModel">古いViewModel。</param>
	/// <param name="newViewModel">新しいViewModel。</param>
	protected override void OnViewModelChanged(IMediaItemViewModel? oldViewModel, IMediaItemViewModel? newViewModel) {
		if (newViewModel is null) {
			this.Player.Stop();
			return;
		}

		this.ThumbnailVisibility.Value = Visibility.Visible;
		this.Player.Open(newViewModel.FilePath);
		this.Player.Pause();
		base.OnViewModelChanged(oldViewModel, newViewModel);
	}

	/// <summary>
	/// コントロールがアンロードされた際の処理。再生を停止します。
	/// </summary>
	/// <param name="sender">イベント発生元。</param>
	/// <param name="e">イベント引数。</param>
	private void OnUnloaded(object sender, RoutedEventArgs e) {
		this.Player.Stop();
	}

	/// <summary>
	/// プレイヤーのプロパティ変更イベントハンドラ。再生・一時停止ボタンの表示状態を更新します。
	/// </summary>
	/// <param name="sender">イベント発生元。</param>
	/// <param name="e">イベント引数。</param>
	private void Player_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e) {
		switch (e.PropertyName) {
			case nameof(this.Player.Status):
				this.DispatcherQueue?.TryEnqueue(() => {
					this.btnPlayback.Content = this.Player.Status == Status.Playing ? this._iconPause : this._iconPlay;

					if (this.Player.Status == Status.Playing) {
						this.ThumbnailVisibility.Value = Visibility.Collapsed;
					}
				});

				break;
		}
	}
}

public class VideoDetailViewerPreviewControlViewUserControl : UserControlBase<IMediaItemViewModel> {
}