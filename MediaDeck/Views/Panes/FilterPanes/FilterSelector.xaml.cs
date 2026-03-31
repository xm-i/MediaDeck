using CommunityToolkit.Mvvm.DependencyInjection;

using MediaDeck.ViewModels.Panes.FilterPanes;
using MediaDeck.Views.Filters;

using Microsoft.UI.Xaml;

namespace MediaDeck.Views.Panes.FilterPanes;

public sealed partial class FilterSelector {
	public FilterSelector() {
		this.InitializeComponent();
	}

	private void OpenFilterSettingsWindowButton_Click(object sender, RoutedEventArgs e) {
		var window = Ioc.Default.GetRequiredService<FilterManagerWindow>();
		window.ActivateCenteredOnMainWindow();
	}
}

public abstract class FilterSelectorUserControl : UserControlBase<FilterSelectorViewModel>;