using MediaDeck.Composition.Bases;
using MediaDeck.Models.Preferences.CustomConfigs;
using MediaDeck.Models.Preferences.CustomConfigs.Objects;

namespace MediaDeck.ViewModels.Preferences.CustomConfigs;

[AddTransient]
public class ScanConfigPageViewModel : ViewModelBase, IConfigPageViewModel {
	private readonly ScanConfig _scanConfig;
	private readonly ObservableList<ExtensionConfig> _targetExtensions = [];
	public ScanConfigPageViewModel(ScanConfig scanConfig) {
		this._scanConfig = scanConfig;
		this.AddExtensionCommand.Subscribe(_ => {
			this._targetExtensions.Add(new ExtensionConfig());
		});
		this.TargetExtensions = this._targetExtensions.ToNotifyCollectionChanged(SynchronizationContextCollectionEventDispatcher.Current);
	}

	/// <summary>
	/// 対象拡張子
	/// </summary>
	public INotifyCollectionChangedSynchronizedViewList<ExtensionConfig> TargetExtensions {
		get;
	}

	public ReactiveCommand AddExtensionCommand {
		get;
	} = new();

	public void Save() {
		this._scanConfig.TargetExtensions.Clear();
		foreach (var te in this.TargetExtensions) {
			this._scanConfig.TargetExtensions.Add(te);
		}
	}

	public void Load() {
		this._targetExtensions.AddRange(this._scanConfig.TargetExtensions);
	}
}
