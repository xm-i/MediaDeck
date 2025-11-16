using MediaDeck.Composition.Bases;
using MediaDeck.FileTypes.Base.Views.Interfaces;
using MediaDeck.FileTypes.Video.ViewModels;

namespace MediaDeck.FileTypes.Video.Views;
public sealed partial class VideoThumbnailPickerView : VideoThumbnailPickerViewUserControl, IThumbnailPickerView {
	public VideoThumbnailPickerView() {
		this.InitializeComponent();
		this.MediaPlayerElement.Loaded += this.MediaPlayerElement_Loaded;
	}

	private void MediaPlayerElement_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e) {
		this.MediaPlayerElement.MediaPlayer.PlaybackSession.PositionChanged += this.PlaybackSession_PositionChanged;
	}

	private void PlaybackSession_PositionChanged(Windows.Media.Playback.MediaPlaybackSession sender, object args) {
		if (this.ViewModel is not { } vm) {
			return;
		}
		vm.UpdateTime(sender.Position);
	}
}

public class VideoThumbnailPickerViewUserControl : UserControlBase<VideoThumbnailPickerViewModel> {
}
