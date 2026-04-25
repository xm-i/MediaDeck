using MediaDeck.Common.Base;
using MediaDeck.Composition.Interfaces.MediaItemTypes.ViewModels;
using MediaDeck.Core.Models.Files;

namespace MediaDeck.ViewModels.Panes.ViewerPanes;

public abstract class ViewerPaneViewModelBase(string name, FilesManager filesManager) : ViewModelBase {
	public string Name {
		get;
	} = name;

	/// <summary>
	/// このViewerが選択されているかどうか。
	/// </summary>
	public BindableReactiveProperty<bool> IsSelected {
		get;
	} = new(false);

	public async Task RemoveFilesAsync(IEnumerable<IMediaItemViewModel> fileViewModels) {
		await filesManager.RemoveFilesAsync(fileViewModels.Select(x => x.FileModel));
	}
}