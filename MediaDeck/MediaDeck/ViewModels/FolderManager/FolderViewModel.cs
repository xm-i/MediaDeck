using MediaDeck.Composition.Bases;
using MediaDeck.Models.FolderManager;

namespace MediaDeck.ViewModels.FolderManager;

[AddTransient]
public class FolderViewModel(FolderModel folderModel) : ViewModelBase {
	private readonly FolderModel _folderModel = folderModel;
	public string FolderPath {
		get {
			return this._folderModel.FolderPath;
		}
	}

	public FolderModel GetModel() {
		return this._folderModel;
	}

}
