using MediaDeck.Composition.Interfaces.MediaItemTypes.ViewModels;
using MediaDeck.MediaItemTypes.UI.Base.Views;
using MediaDeck.MediaItemTypes.Video.ViewModels;

namespace MediaDeck.MediaItemTypes.UI.Video.Views;

public sealed partial class VideoThumbnailControlView : VideoThumbnailControlViewUserControl {
	public VideoThumbnailControlView() {
		this.InitializeComponent();
	}

	public VideoMediaItemViewModel? CastedViewModel {
		get {
			return this.ViewModel as VideoMediaItemViewModel;
		}
	}

	protected override void OnViewModelChanged(IMediaItemViewModel? oldViewModel, IMediaItemViewModel? newViewModel) {
		base.OnViewModelChanged(oldViewModel, newViewModel);
		this.Bindings.Update();
	}
}

public class VideoThumbnailControlViewUserControl : UserControlBase<IMediaItemViewModel> {
}