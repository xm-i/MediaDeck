using MediaDeck.ViewModels.Tools;

namespace MediaDeck.Views.Tools;

[Inject(InjectServiceLifetime.Transient)]
public sealed partial class DuplicateDetectorWindow {
	public DuplicateDetectorWindow(DuplicateDetectorViewModel duplicateDetectorViewModel) {
		this.InitializeComponent();
		this.ViewModel = duplicateDetectorViewModel;
		this.AppWindow.Resize(new(900, 600));
	}

	public DuplicateDetectorViewModel ViewModel {
		get;
	}
}