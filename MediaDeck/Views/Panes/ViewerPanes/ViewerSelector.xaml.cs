using CommunityToolkit.Mvvm.DependencyInjection;

using MediaDeck.Composition.Interfaces.Services;
using MediaDeck.ViewModels.Panes.ViewerPanes;
using MediaDeck.Views.Sort;

using Microsoft.UI.Xaml;

namespace MediaDeck.Views.Panes.ViewerPanes;

public sealed partial class ViewerSelector {
	public ViewerSelector() {
		this.InitializeComponent();
		this.Loaded += (_, _) => {
			this.Bindings.Update();
		};
	}
	private void Button_Click(object sender, RoutedEventArgs e) {
		var window = Ioc.Default.GetRequiredService<SortManagerWindow>();
		Ioc.Default.GetRequiredService<IWindowService>().ActivateCenteredOnMainWindow(window);
	}

	private void Segmented_SelectionChanged(object sender, Microsoft.UI.Xaml.Controls.SelectionChangedEventArgs e) {
		if (this.ViewModel == null) {
			return;
		}
		if (e.AddedItems.Count != 1 || e.AddedItems[0] is not ViewerPaneViewModelBase selectedViewerPane) {
			return;
		}

		this.ViewModel.SelectedViewerPane.Value = selectedViewerPane;
	}
}


public abstract class ViewerSelectorUserControl : UserControlBase<ViewerSelectorViewModel>;