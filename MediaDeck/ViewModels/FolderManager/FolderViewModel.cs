using MediaDeck.Composition.Bases;
using MediaDeck.Composition.Stores.State.Model.Objects;

namespace MediaDeck.ViewModels.FolderManager;

[Inject(InjectServiceLifetime.Transient)]
public class FolderViewModel : ViewModelBase {
	private readonly FolderModel _folderModel;

	public string FolderPath {
		get {
			return this._folderModel.FolderPath;
		}
	}

	public BindableReactiveProperty<bool> IsScanning {
		get;
	}

	public BindableReactiveProperty<long> TotalCount {
		get;
	}

	public BindableReactiveProperty<long> RemainingCount {
		get;
	}

	public BindableReactiveProperty<long> ProcessedCount {
		get;
	}

	public FolderViewModel(FolderModel folderModel) {
		this._folderModel = folderModel;
		this.IsScanning = folderModel.IsScanning.ToBindableReactiveProperty();
		this.TotalCount = folderModel.TotalCount.ToBindableReactiveProperty();
		this.RemainingCount = folderModel.RemainingCount.Debounce(TimeSpan.FromMilliseconds(500)).ToBindableReactiveProperty();
		this.ProcessedCount = folderModel.TotalCount
			.CombineLatest(folderModel.RemainingCount, (total, remaining) => total - remaining)
			.ToBindableReactiveProperty();
	}

	public FolderModel GetModel() {
		return this._folderModel;
	}
}
