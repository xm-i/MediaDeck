using MediaDeck.Common.Base;
using MediaDeck.Composition.Stores.Config.Model;
using MediaDeck.Composition.Stores.Config.Model.Objects;
using MediaDeck.Core.Models.Files;

namespace MediaDeck.Core.Models.FolderManager;

[Inject(InjectServiceLifetime.Transient)]
public class FolderManagerModel : ModelBase {
	private readonly FileRegistrar _fileRegistrar;
	private readonly FolderManagerConfigModel _folderManagerConfig;

	public FolderManagerModel(FileRegistrar fileRegistrar, FolderManagerConfigModel folderManagerConfig) {
		this._fileRegistrar = fileRegistrar;
		this._folderManagerConfig = folderManagerConfig;
		this.Folders = this._folderManagerConfig.Folders;
	}

	public ObservableList<FolderModel> Folders {
		get;
	} = [];

	public void AddFolder(string folderPath) {
		this._folderManagerConfig.Folders.Add(new FolderModel() { FolderPath = folderPath });
	}

	public void RemoveFolder(FolderModel folder) {
		this._folderManagerConfig.Folders.Remove(folder);
	}

	public async Task Scan() {
		foreach (var folder in this.Folders.ToArray()) {
			await this._fileRegistrar.ScanFolderAsync(folder);
		}
	}

	public async Task ScanFolder(FolderModel folder) {
		await this._fileRegistrar.ScanFolderAsync(folder);
	}
}