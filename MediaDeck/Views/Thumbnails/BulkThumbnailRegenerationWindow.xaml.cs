using MediaDeck.ViewModels.Thumbnails;

namespace MediaDeck.Views.Thumbnails;

[Inject(InjectServiceLifetime.Transient)]
public sealed partial class BulkThumbnailRegenerationWindow {
	public BulkThumbnailRegenerationWindow(BulkThumbnailRegenerationViewModel viewModel) {
		this.InitializeComponent();
		this.ViewModel = viewModel;
		this.AppWindow.Resize(new(1100, 700));
	}

	public BulkThumbnailRegenerationViewModel ViewModel {
		get;
	}
}