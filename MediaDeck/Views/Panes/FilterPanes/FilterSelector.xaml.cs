using CommunityToolkit.Mvvm.DependencyInjection;

using MediaDeck.Composition.Interfaces.Services;
using MediaDeck.ViewModels.Panes.FilterPanes;
using MediaDeck.Views.Filters;

using Microsoft.UI.Xaml;

namespace MediaDeck.Views.Panes.FilterPanes;

public sealed partial class FilterSelector {
	public FilterSelector() {
		this.InitializeComponent();
		this.Loaded += (_, _) => {
			this.Bindings.Update();
		};
	}

	private void OpenFilterSettingsWindowButton_Click(object sender, RoutedEventArgs e) {
		var window = Ioc.Default.GetRequiredService<FilterManagerWindow>();
		Ioc.Default.GetRequiredService<IWindowService>().ActivateCenteredOnMainWindow(window);
	}
}


public abstract class FilterSelectorUserControl : UserControlBase<FilterSelectorViewModel>;