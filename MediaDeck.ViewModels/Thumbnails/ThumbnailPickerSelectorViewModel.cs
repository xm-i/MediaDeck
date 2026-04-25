using MediaDeck.Common.Base;
using MediaDeck.Composition.Interfaces.MediaItemTypes;
using MediaDeck.Composition.Interfaces.MediaItemTypes.ViewModels;
using MediaDeck.Composition.Interfaces.MediaItemTypes.Views;

namespace MediaDeck.ViewModels.Thumbnails;

[Inject(InjectServiceLifetime.Transient)]
public class ThumbnailPickerSelectorViewModel : ViewModelBase {
	public ThumbnailPickerSelectorViewModel(IMediaItemTypeService MediaItemTypeService) {
		this.FileViewModel.Subscribe(async x => {
			if (x == null) {
				this.ThumbnailPickerViewModel.Value = null;
				this.ThumbnailPickerView.Value = null;
				return;
			}
			this.ThumbnailPickerViewModel.Value = MediaItemTypeService.CreateThumbnailPickerViewModel(x);
			this.ThumbnailPickerView.Value = MediaItemTypeService.CreateThumbnailPickerView(x);
			this.ThumbnailPickerView.Value.DataContext = this.ThumbnailPickerViewModel.Value;
			await this.ThumbnailPickerViewModel.Value.LoadAsync(x);
		}).AddTo(this.CompositeDisposable);
	}


	public BindableReactiveProperty<IThumbnailPickerViewModel?> ThumbnailPickerViewModel {
		get;
	} = new();

	public BindableReactiveProperty<IThumbnailPickerView?> ThumbnailPickerView {
		get;
	} = new();

	public BindableReactiveProperty<IMediaItemViewModel> FileViewModel {
		get;
	} = new();
}