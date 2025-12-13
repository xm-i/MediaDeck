using Microsoft.UI.Xaml;

using MediaDeck.ViewModels.Filters;

using Windows.Graphics;
namespace MediaDeck.Views.Filters;
[Inject(InjectServiceLifetime.Transient)]
public sealed partial class FilterManagerWindow : Window {
	public FilterManagerWindow(FilterManagerViewModel filterManagerViewModel) {
		this.InitializeComponent();
		this.ViewModel = filterManagerViewModel;
		this.AppWindow.Resize(new SizeInt32(1000, 700));
	}

	public FilterManagerViewModel ViewModel {
		get;
	}
}
