using MediaDeck.Common.Base;
using MediaDeck.Common.Extensions;
using MediaDeck.Composition.Stores.State.Model;

namespace MediaDeck.ViewModels.Panes.ViewerPanes;

[Inject(InjectServiceLifetime.Scoped)]
public class ViewerSelectorViewModel : ViewModelBase {
	public ViewerSelectorViewModel(
		TabStateModel tabState,
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

		this.ItemSize = tabState.ViewerState.ItemSize.ToTwoWayBindableReactiveProperty(tabState.ViewerState.ItemSize.Value, this.CompositeDisposable).AddTo(this.CompositeDisposable);

		this.SelectedViewerPane.Pairwise()
			.Subscribe(x => {
				x.Previous?.IsSelected.Value = false;
				x.Current?.IsSelected.Value = true;
			})
			.AddTo(this.CompositeDisposable);

		this.RefreshCommand.Subscribe(x => this.MediaContentLibraryViewModel.Reload()).AddTo(this.CompositeDisposable);
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

	public ReactiveCommand RefreshCommand {
		get;
	} = new();
}