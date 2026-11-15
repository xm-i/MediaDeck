using FlyleafLib.MediaPlayer;
using MediaDeck.Composition.Interfaces.MediaItemTypes.ViewModels;
using MediaDeck.MediaItemTypes.UI.Base.Views;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Imaging;

namespace MediaDeck.MediaItemTypes.UI.Video.Views;

/// <summary>
/// ビデオプレビュー詳細ビューのコントロール。
/// </summary>
public sealed partial class VideoDetailViewerPreviewControlView : VideoDetailViewerPreviewControlViewUserControl {
	/// <summary>
	/// プレイヤーインスタンスを取得します。
	/// </summary>
	public Player Player {
		get {
			return this.PlayerControl.Player;
		}
	}

	/// <summary>
	/// <see cref="VideoDetailViewerPreviewControlView"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	public VideoDetailViewerPreviewControlView() {
		this.InitializeComponent();
		this.Unloaded += this.OnUnloaded;
	}

	/// <summary>
	/// ViewModelが変更された際の処理。ビデオを停止または新規オープンします。
	/// </summary>
	/// <param name="oldViewModel">古いViewModel。</param>
	/// <param name="newViewModel">新しいViewModel。</param>
	protected override void OnViewModelChanged(IMediaItemViewModel? oldViewModel, IMediaItemViewModel? newViewModel) {
		if (newViewModel is null) {
			this.PlayerControl.Player.Stop();
			return;
		}

		if (!string.IsNullOrEmpty(newViewModel.ThumbnailFilePath.Value)) {
			this.PlayerControl.ThumbnailSource = new BitmapImage(new Uri(newViewModel.ThumbnailFilePath.Value));
		} else {
			this.PlayerControl.ThumbnailSource = null;
		}

		this.PlayerControl.Player.Open(newViewModel.FilePath);
		this.PlayerControl.Player.Pause();
		base.OnViewModelChanged(oldViewModel, newViewModel);
	}

	/// <summary>
	/// コントロールがアンロードされた際の処理。再生を停止します。
	/// </summary>
	/// <param name="sender">イベント発生元。</param>
	/// <param name="e">イベント引数。</param>
	private void OnUnloaded(object sender, RoutedEventArgs e) {
		this.PlayerControl.Player.Stop();
	}
}

/// <summary>
/// <see cref="VideoDetailViewerPreviewControlView"/> のジェネリック基底クラス。
/// </summary>
public class VideoDetailViewerPreviewControlViewUserControl : UserControlBase<IMediaItemViewModel>;