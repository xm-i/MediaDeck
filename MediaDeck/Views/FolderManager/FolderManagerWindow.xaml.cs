using MediaDeck.ViewModels.FolderManager;

namespace MediaDeck.Views.FolderManager;

[Inject(InjectServiceLifetime.Transient)]
public sealed partial class FolderManagerWindow {
	public FolderManagerViewModel ViewModel {
		get;
	}

	public FolderManagerWindow(FolderManagerViewModel viewModel) {
		this.InitializeComponent();
		this.ViewModel = viewModel;
		this.AppWindow.Resize(new(1000, 700));
	}
}