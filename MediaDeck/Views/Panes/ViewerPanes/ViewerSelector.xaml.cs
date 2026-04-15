using CommunityToolkit.Mvvm.DependencyInjection;

using MediaDeck.Composition.Interfaces.Services;
using MediaDeck.ViewModels.Panes.ViewerPanes;
using MediaDeck.Views.Sort;

using Microsoft.UI.Xaml;

namespace MediaDeck.Views.Panes.ViewerPanes;

public sealed partial class ViewerSelector {
	public ViewerSelector() {
		this.InitializeComponent();
	}

	private void Button_Click(object sender, RoutedEventArgs e) {
		var window = Ioc.Default.GetRequiredService<SortManagerWindow>();
		Ioc.Default.GetRequiredService<IWindowService>().ActivateCenteredOnMainWindow(window);
	}
}


public abstract class ViewerSelectorUserControl : UserControlBase<ViewerSelectorViewModel>;