using CommunityToolkit.Mvvm.DependencyInjection;

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
		window.ActivateCenteredOnMainWindow();
	}
}

public abstract class ViewerSelectorUserControl : UserControlBase<ViewerSelectorViewModel>;