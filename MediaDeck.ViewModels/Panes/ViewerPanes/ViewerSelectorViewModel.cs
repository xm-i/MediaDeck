using MediaDeck.Common.Base;
using MediaDeck.Common.Extensions;
using MediaDeck.Composition.Stores.State.Model;

namespace MediaDeck.ViewModels.Panes.ViewerPanes;

[Inject(InjectServiceLifetime.Scoped)]
public class ViewerSelectorViewModel : ViewModelBase {
	public ViewerSelectorViewModel(
		TabStateModel tabState,
		MediaContentLibraryViewModel mediaContentLibraryViewModel,
		SearchConditionManagerViewModel searchConditionManagerViewModel,
		WrapViewerViewModel wrapViewerViewModel,
		ListViewerViewModel listViewerViewModel,
		DetailViewerViewModel detailViewerViewModel,
		MapViewerViewModel mapViewerViewModel) {
		this.MediaContentLibraryViewModel = mediaContentLibraryViewModel;
		this.SearchConditionManagerViewModel = searchConditionManagerViewModel;
		this.ViewerPaneViewModels = [
			wrapViewerViewModel,
			listViewerViewModel,
			detailViewerViewModel,
			mapViewerViewModel
		];
		this.WrapViewerViewModel = wrapViewerViewModel;
		this.ListViewerViewModel = listViewerViewModel;
		this.DetailViewerViewModel = detailViewerViewModel;
		this.MapViewerViewModel = mapViewerViewModel;

		this.SelectedViewerPane.Value = this.ViewerPaneViewModels.FirstOrDefault(x => x.ViewerType == tabState.ViewerState.ActiveViewer.Value) ?? wrapViewerViewModel;

		this.ItemSize = tabState.ViewerState.ItemSize.ToTwoWayBindableReactiveProperty(tabState.ViewerState.ItemSize.Value, this.CompositeDisposable).AddTo(this.CompositeDisposable);

		this.ShowOverlay = tabState.ViewerState.ShowOverlay.ToTwoWayBindableReactiveProperty(tabState.ViewerState.ShowOverlay.Value, this.CompositeDisposable).AddTo(this.CompositeDisposable);
		this.ShowInfo = tabState.ViewerState.ShowInfo.ToTwoWayBindableReactiveProperty(tabState.ViewerState.ShowInfo.Value, this.CompositeDisposable).AddTo(this.CompositeDisposable);

		this.SelectedViewerPane.Pairwise()
			.Subscribe(x => {
				x.Previous?.IsSelected.Value = false;
				x.Current?.IsSelected.Value = true;
				if (x.Current is not null) {
					tabState.ViewerState.ActiveViewer.Value = x.Current.ViewerType;
				}
			})
			.AddTo(this.CompositeDisposable);

	}

	public MediaContentLibraryViewModel MediaContentLibraryViewModel {
		get;
	}

	public SearchConditionManagerViewModel SearchConditionManagerViewModel {
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

	public BindableReactiveProperty<int> ItemSize {
		get;
	} = new(150);

	public BindableReactiveProperty<bool> ShowOverlay {
		get;
	}

	public BindableReactiveProperty<bool> ShowInfo {
		get;
	}

}