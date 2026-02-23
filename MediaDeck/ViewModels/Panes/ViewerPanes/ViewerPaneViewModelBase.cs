using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using MediaDeck.Composition.Bases;
using MediaDeck.FileTypes.Base.ViewModels.Interfaces;
using MediaDeck.Models.FileDetailManagers;

namespace MediaDeck.ViewModels.Panes.ViewerPanes;

public abstract class ViewerPaneViewModelBase(string name, FilesManager filesManager) : ViewModelBase {
	public string Name {
		get;
	} = name;

	public async Task RemoveFilesAsync(IEnumerable<IFileViewModel> fileViewModels) {
		await filesManager.RemoveFilesAsync(fileViewModels.Select(x => x.FileModel));
	}
}
