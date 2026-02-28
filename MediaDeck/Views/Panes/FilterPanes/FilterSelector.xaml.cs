using CommunityToolkit.Mvvm.DependencyInjection;

using Microsoft.UI.Xaml;

using MediaDeck.Composition.Bases;
using MediaDeck.Utils.Tools;
using MediaDeck.ViewModels.Panes.FilterPanes;
using MediaDeck.Views.Filters;

namespace MediaDeck.Views.Panes.FilterPanes;
public sealed partial class FilterSelector : FilterSelectorUserControl {
	public FilterSelector() {
		this.InitializeComponent();
	}

	private void OpenFilterSettingsWindowButton_Click(object sender, RoutedEventArgs e) {
		var window = Ioc.Default.GetRequiredService<FilterManagerWindow>();
		window.ActivateCenteredOnMainWindow();
	}
}
public abstract class FilterSelectorUserControl : UserControlBase<FilterSelectorViewModel>;

