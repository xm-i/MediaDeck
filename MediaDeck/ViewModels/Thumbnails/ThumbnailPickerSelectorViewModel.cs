using MediaDeck.Composition.Bases;
using MediaDeck.FileTypes.Base.ViewModels.Interfaces;
using MediaDeck.FileTypes.Base.Views.Interfaces;

namespace MediaDeck.ViewModels.Thumbnails;
[AddTransient]
public class ThumbnailPickerSelectorViewModel: ViewModelBase {

	public ThumbnailPickerSelectorViewModel() {

		this.FileViewModel.Subscribe(async x => {
			if(x == null) {
				this.ThumbnailPickerViewModel.Value = null;
				this.ThumbnailPickerView.Value = null;
				return;
			}
			this.ThumbnailPickerViewModel.Value = FileTypeUtility.CreateThumbnailPickerViewModel(x);
			this.ThumbnailPickerView.Value = FileTypeUtility.CreateThumbnailPickerView(x);
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
