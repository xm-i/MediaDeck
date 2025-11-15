using MediaDeck.Composition.Bases;

namespace MediaDeck.ViewModels.Panes.ViewerPanes;

[AddTransient]
public class ViewerSelectorViewModel : ViewModelBase {
	public ViewerSelectorViewModel(
		MediaContentLibraryViewModel mediaContentLibraryViewModel,
		WrapViewerViewModel wrapViewerViewModel,
		ListViewerViewModel listViewerViewModel,
		DetailViewerViewModel detailViewerViewModel,
		MapViewerViewModel mapViewerViewModel,
		SortSelectorViewModel sortSelectorViewModel) {
		this.MediaContentLibraryViewModel = mediaContentLibraryViewModel;
		this.ViewerPaneViewModels = [
			wrapViewerViewModel,
			listViewerViewModel,
			detailViewerViewModel,
			mapViewerViewModel
		];
		this.SelectedViewerPane.Value = wrapViewerViewModel;
		this.WrapViewerViewModel = wrapViewerViewModel;
		this.ListViewerViewModel = listViewerViewModel;
		this.DetailViewerViewModel = detailViewerViewModel;
		this.MapViewerViewModel = mapViewerViewModel;
		this.SortSelectorViewModel = sortSelectorViewModel;
	}

	public MediaContentLibraryViewModel MediaContentLibraryViewModel {
		get;
	}

	public BindableReactiveProperty<ViewerPaneViewModelBase> SelectedViewerPane {
		get;
	} = new();

	public ViewerPaneViewModelBase[] ViewerPaneViewModels {
		get;
	}
	public WrapViewerViewModel WrapViewerViewModel {
		get;
	}
	public ListViewerViewModel ListViewerViewModel {
		get;
	}
	public DetailViewerViewModel DetailViewerViewModel {
		get;
	}
	public MapViewerViewModel MapViewerViewModel {
		get;
	}
	public SortSelectorViewModel SortSelectorViewModel {
		get;
	}

	public BindableReactiveProperty<int> ItemSize {
		get;
	} = new(150);
}
