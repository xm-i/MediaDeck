using MediaDeck.ViewModels.Sort;

namespace MediaDeck.Views.Sort;

[Inject(InjectServiceLifetime.Transient)]
public sealed partial class SortManagerWindow {
	public SortManagerWindow(SortManagerViewModel sortManagerViewModel) {
		this.InitializeComponent();
		this.ViewModel = sortManagerViewModel;
		this.ViewModel.LoadCommand.Execute(Unit.Default);
		this.AppWindow.Resize(new(1000, 700));
	}

	public SortManagerViewModel ViewModel {
		get;
	}
}