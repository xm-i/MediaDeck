using Microsoft.UI.Xaml.Controls;

using MediaDeck.Composition.Bases;
using MediaDeck.ViewModels.Filters;
using MediaDeck.ViewModels.Filters.FilterItemCreators;
using MediaDeck.Views.Filters.FilterItemCreators;

namespace MediaDeck.Views.Filters;
public sealed partial class FilterManagerView: FilterManagerViewUserControl {
	public FilterManagerView() {
		this.InitializeComponent();
	}

	private void FilterCreatorComboBox_SelectionChanged(object sender, SelectionChangedEventArgs args) {
		if (args.AddedItems.FirstOrDefault() is not IFilterCreatorViewModel filterCreatorViewModel) {
			return;
		}

		switch (filterCreatorViewModel) {
			case ExistsFilterCreatorViewModel:
				this.ContentFrame.Navigate(typeof(ExistsFilter));
				break;
			case FilePathFilterCreatorViewModel:
				this.ContentFrame.Navigate(typeof(FilePathFilter));
				break;
			case LocationFilterCreatorViewModel:
				this.ContentFrame.Navigate(typeof(LocationFilter));
				break;
			case MediaTypeFilterCreatorViewModel:
				this.ContentFrame.Navigate(typeof(MediaTypeFilter));
				break;
			case RateFilterCreatorViewModel:
				this.ContentFrame.Navigate(typeof(RateFilter));
				break;
			case ResolutionFilterCreatorViewModel:
				this.ContentFrame.Navigate(typeof(ResolutionFilter));
				break;
			case TagFilterCreatorViewModel:
				this.ContentFrame.Navigate(typeof(TagFilter));
				break;
		}
	}
}

public class FilterManagerViewUserControl: UserControlBase<FilterManagerViewModel> {
}