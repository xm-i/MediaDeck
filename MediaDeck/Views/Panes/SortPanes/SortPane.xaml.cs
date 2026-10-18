using System.ComponentModel;

using CommunityToolkit.Mvvm.DependencyInjection;

using MediaDeck.Services;
using MediaDeck.ViewModels.Panes.SortPanes;
using MediaDeck.Views.Sort;

using Microsoft.UI.Xaml;

namespace MediaDeck.Views.Panes.SortPanes;

/// <summary>
/// ソートペインのビュー。
/// </summary>
public sealed partial class SortPane {
	/// <summary>
	/// <see cref="SortPane"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	public SortPane() {
		this.InitializeComponent();
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

	private void CurrentCondition_SelectionChanged(object sender, Microsoft.UI.Xaml.Controls.SelectionChangedEventArgs e) {
		var selected = e.AddedItems.FirstOrDefault();
		if (selected is not SortConditionViewModel scvm) {
			return;
		}
		this.ViewModel?.CurrentCondition.Value = scvm;
	}


	private void DirectionToggleButton_Checked(object sender, RoutedEventArgs e) {
		this.ViewModel?.Direction.Value = ListSortDirection.Ascending;
	}

	private void DirectionToggleButton_Unchecked(object sender, RoutedEventArgs e) {
		this.ViewModel?.Direction.Value = ListSortDirection.Descending;
	}
}


public abstract class SortPaneUserControl : UserControlBase<SortSelectorViewModel>;