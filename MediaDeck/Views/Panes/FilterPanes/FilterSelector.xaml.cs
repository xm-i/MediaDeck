using CommunityToolkit.Mvvm.DependencyInjection;

using MediaDeck.Composition.Interfaces.Services;
using MediaDeck.ViewModels.Panes.FilterPanes;
using MediaDeck.Views.Filters;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace MediaDeck.Views.Panes.FilterPanes;

public sealed partial class FilterSelector {
	private bool _isSyncingSelection;
	private IDisposable? _selectedConditionsSubscription;

	public FilterSelector() {
		this.InitializeComponent();
		this.Loaded += this.FilterSelector_Loaded;
	}

	private void FilterSelector_Loaded(object sender, RoutedEventArgs e) {
		this.Bindings.Update();
		this.SyncListSelectionFromViewModel(this.ViewModel.SelectedConditions.Value);
		this._selectedConditionsSubscription?.Dispose();
		this._selectedConditionsSubscription = this.ViewModel.SelectedConditions
			.Subscribe(this.SyncListSelectionFromViewModel);
	}

	private void SyncListSelectionFromViewModel(FilteringConditionViewModel[]? selected) {
		var selectedArray = selected ?? [];
		this._isSyncingSelection = true;
		try {
			this.FilteringCondisionListBox.SelectedItems.Clear();
			foreach (var item in selectedArray) {
				this.FilteringCondisionListBox.SelectedItems.Add(item);
			}
		} finally {
			this._isSyncingSelection = false;
		}
	}

	private void FilteringCondisionListBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
		if (this._isSyncingSelection) {
			return;
		}
		if (this.ViewModel == null) {
			return;
		}
		if (sender is not ListView listView) {
			return;
		}
		var selected = listView.SelectedItems.OfType<FilteringConditionViewModel>().ToArray();
		this.ViewModel.ChangeFilteringConditionSelectionCommand.Execute(selected);
	}

	private void OpenFilterSettingsWindowButton_Click(object sender, RoutedEventArgs e) {
		var window = Ioc.Default.GetRequiredService<FilterManagerWindow>();
		Ioc.Default.GetRequiredService<IWindowService>().ActivateCenteredOnMainWindow(window);
	}

	protected override void OnViewModelChanged(FilterSelectorViewModel? oldViewModel, FilterSelectorViewModel? newViewModel) {
		base.OnViewModelChanged(oldViewModel, newViewModel);
		this._selectedConditionsSubscription?.Dispose();
		this._selectedConditionsSubscription = null;
	}
}


public abstract class FilterSelectorUserControl : UserControlBase<FilterSelectorViewModel>;