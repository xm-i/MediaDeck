using CommunityToolkit.Mvvm.DependencyInjection;

using MediaDeck.Services;
using MediaDeck.ViewModels.Panes.ViewerPanes;
using MediaDeck.Views.Sort;

using Microsoft.UI.Xaml;

namespace MediaDeck.Views.Panes.SortPanes;

public sealed partial class SortPane {
	public SortPane() {
		this.InitializeComponent();
		this.Loaded += (_, _) => {
			this.Bindings.Update();
		};
	}

	private void OpenSortSettingsWindowButton_Click(object sender, RoutedEventArgs e) {
		var window = Ioc.Default.GetRequiredService<SortManagerWindow>();
		var windowManager = Ioc.Default.GetRequiredService<WindowManager>();
		var parent = windowManager.GetWindowFromElement(this);
		if (parent == null) {
			// TODO: notify
			return;
		}
		Ioc.Default.GetRequiredService<WindowService>().ActivateCenteredOnMainWindow(window, parent);
	}
}


public abstract class SortPaneUserControl : UserControlBase<SortSelectorViewModel>;
