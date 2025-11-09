using CommunityToolkit.Mvvm.DependencyInjection;

using MediaDeck.Composition.Bases;
using MediaDeck.ViewModels.Panes.ViewerPanes;
using MediaDeck.Views.Sort;

namespace MediaDeck.Views.Panes.ViewerPanes;
public sealed partial class ViewerSelector : ViewerSelectorUserControl {
	public ViewerSelector() {
		this.InitializeComponent();
	}

	private void Button_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e) {
		var window = Ioc.Default.GetRequiredService<SortManagerWindow>();
		window.Activate();
	}
}
public abstract class ViewerSelectorUserControl: UserControlBase<ViewerSelectorViewModel>;
