using Windows.Media.Playback;

using MediaDeck.Composition.Interfaces.FileTypes.Views;
using MediaDeck.FileTypes.Base.Views;
using MediaDeck.FileTypes.Video.ViewModels;

using Microsoft.UI.Xaml;

namespace MediaDeck.FileTypes.Video.Views;

internal sealed partial class VideoThumbnailPickerView : VideoThumbnailPickerViewUserControl, IThumbnailPickerView {
	internal VideoThumbnailPickerView() {
		this.InitializeComponent();
		this.MediaPlayerElement.Loaded += this.MediaPlayerElement_Loaded;
	}

	private void MediaPlayerElement_Loaded(object sender, RoutedEventArgs e) {
		this.MediaPlayerElement.MediaPlayer.PlaybackSession.PositionChanged += this.PlaybackSession_PositionChanged;
	}

	private void PlaybackSession_PositionChanged(MediaPlaybackSession sender, object args) {
		if (this.ViewModel is not { } vm) {
			return;
		}
		vm.UpdateTime(sender.Position);
	}
}

internal class VideoThumbnailPickerViewUserControl : UserControlBase<VideoThumbnailPickerViewModel> { }