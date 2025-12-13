using System.IO;
using System.Threading.Tasks;

using MediaDeck.Composition.Bases;
using MediaDeck.Composition.Stores.State.Model;
using MediaDeck.Composition.Stores.State.Model.Objects;
using MediaDeck.Models.Files;

namespace MediaDeck.Models.FolderManager;

[Inject(InjectServiceLifetime.Transient)]
public class FolderManagerModel : ModelBase {
	private readonly FileRegistrar _fileRegistrar;
	private readonly FolderManagerStateModel _folderManagerStates;
	public FolderManagerModel(FileRegistrar fileRegistrar, FolderManagerStateModel folderManagerStates) {
		this._fileRegistrar = fileRegistrar;
		this._folderManagerStates = folderManagerStates;
		this.Folders = this._folderManagerStates.Folders;
	}

	public ObservableList<FolderModel> Folders {
		get;
	} = [];

	public void AddFolder(string folderPath) {
		this._folderManagerStates.Folders.Add(new FolderModel() { FolderPath = folderPath });
	}

	public void RemoveFolder(FolderModel folder) {
		this._folderManagerStates.Folders.Remove(folder);
	}

	public async Task Scan() {
		foreach (var folder in this.Folders) {
			var files = Directory.EnumerateFiles(folder.FolderPath, "", SearchOption.AllDirectories);
			await Task.Run(() => {
				this._fileRegistrar.RegistrationQueue.EnqueueRange(files.Where(x => x.IsTargetFile()));
			});
		}
	}
}
