using MediaDeck.Composition.Interfaces.MediaItemTypes.ViewModels;
using MediaDeck.MediaItemTypes.Archive.ViewModels;
using MediaDeck.MediaItemTypes.UI.Base.Views;

namespace MediaDeck.MediaItemTypes.UI.Archive.Views;

public sealed partial class ArchiveThumbnailControlView : ArchiveThumbnailControlViewUserControl {
	public ArchiveThumbnailControlView() {
		this.InitializeComponent();
	}

	public ArchiveMediaItemViewModel? CastedViewModel {
		get {
			return this.ViewModel as ArchiveMediaItemViewModel;
		}
	}

	protected override void OnViewModelChanged(IMediaItemViewModel? oldViewModel, IMediaItemViewModel? newViewModel) {
		base.OnViewModelChanged(oldViewModel, newViewModel);
		this.Bindings.Update();
	}
}

public class ArchiveThumbnailControlViewUserControl : UserControlBase<IMediaItemViewModel> {
}