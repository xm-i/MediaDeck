using MediaDeck.ViewModels.Thumbnails;

namespace MediaDeck.Views.Thumbnails;

[Inject(InjectServiceLifetime.Transient)]
public sealed partial class ThumbnailPickerWindow {
	public ThumbnailPickerWindow(ThumbnailPickerSelectorViewModel thumbnailPickerSelectorViewModel) {
		this.InitializeComponent();
		this.ViewModel = thumbnailPickerSelectorViewModel;
		this.AppWindow.Resize(new(1000, 700));
	}

	public ThumbnailPickerSelectorViewModel ViewModel {
		get;
	}
}