using MediaDeck.Common.Base;
using MediaDeck.Composition.Interfaces.FileTypes;
using MediaDeck.Composition.Interfaces.FileTypes.ViewModels;
using MediaDeck.Composition.Interfaces.FileTypes.Views;

namespace MediaDeck.ViewModels.Thumbnails;

[Inject(InjectServiceLifetime.Transient)]
public class ThumbnailPickerSelectorViewModel : ViewModelBase {
	public ThumbnailPickerSelectorViewModel(IFileTypeService fileTypeService) {
		this.FileViewModel.Subscribe(async x => {
			if (x == null) {
				this.ThumbnailPickerViewModel.Value = null;
				this.ThumbnailPickerView.Value = null;
				return;
			}
			this.ThumbnailPickerViewModel.Value = fileTypeService.CreateThumbnailPickerViewModel(x);
			this.ThumbnailPickerView.Value = fileTypeService.CreateThumbnailPickerView(x);
			this.ThumbnailPickerView.Value.DataContext = this.ThumbnailPickerViewModel.Value;
			await this.ThumbnailPickerViewModel.Value.LoadAsync(x);
		});
	}


	public BindableReactiveProperty<IThumbnailPickerViewModel?> ThumbnailPickerViewModel {
		get;
	} = new();

	public BindableReactiveProperty<IThumbnailPickerView?> ThumbnailPickerView {
		get;
	} = new();

	public BindableReactiveProperty<IFileViewModel> FileViewModel {
		get;
	} = new();
}