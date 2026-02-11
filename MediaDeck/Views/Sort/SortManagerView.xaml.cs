using MediaDeck.Composition.Bases;
using MediaDeck.Composition.Stores.State.Model.Objects;
using MediaDeck.ViewModels.Sort;

using Microsoft.UI.Xaml;

namespace MediaDeck.Views.Sort;
public sealed partial class SortManagerView: SortManagerViewUserControl {
	public SortManagerView() {
		this.InitializeComponent();
	}

	private void RemoveSortItemButton_Click(object sender, RoutedEventArgs e) {
		if (sender is FrameworkElement { Tag: SortItemObject sortItem } &&
			this.ViewModel?.CurrentCondition.Value is { } currentCondition) {
			currentCondition.RemoveSortItemCommand.Execute(sortItem);
		}
	}
}

public class SortManagerViewUserControl : UserControlBase<SortManagerViewModel> {
}