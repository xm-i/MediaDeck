using MediaDeck.Common.Base;
using MediaDeck.Composition.Interfaces.FileTypes.ViewModels;
using MediaDeck.Core.Models.Files;

namespace MediaDeck.ViewModels.Panes.ViewerPanes;

public abstract class ViewerPaneViewModelBase(string name, FilesManager filesManager) : ViewModelBase {
	public string Name {
		get;
	} = name;

	public async Task RemoveFilesAsync(IEnumerable<IFileViewModel> fileViewModels) {
		await filesManager.RemoveFilesAsync(fileViewModels.Select(x => x.FileModel));
	}
}