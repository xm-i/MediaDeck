using MediaDeck.ViewModels.Filters;

namespace MediaDeck.Views.Filters;

[Inject(InjectServiceLifetime.Transient)]
public sealed partial class FilterManagerWindow {
	public FilterManagerWindow(FilterManagerViewModel filterManagerViewModel) {
		this.InitializeComponent();
		this.ViewModel = filterManagerViewModel;
		this.AppWindow.Resize(new(1000, 700));
	}

	public FilterManagerViewModel ViewModel {
		get;
	}
}