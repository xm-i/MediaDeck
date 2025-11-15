using MediaDeck.Composition.Bases;
using MediaDeck.Composition.Stores.Config.Model;

using Microsoft.Extensions.DependencyInjection;

namespace MediaDeck.ViewModels.Preferences.Config;

[AddTransient]
public class ScanConfigPageViewModel : ViewModelBase, IConfigPageViewModel {
	public string PageName {
		get;
	} = "Scan";

	private readonly ScanConfigModel _scanConfig;
	public ScanConfigPageViewModel(ScanConfigModel scanConfig) {
		this._scanConfig = scanConfig;
		this.AddExtensionCommand.Subscribe(_ => {
			this._scanConfig.AddTargetExtension();
		});
		this.TargetExtensions =
			this._scanConfig
				.TargetExtensions
				.CreateView(x => x.ScopedServiceProvider.GetRequiredService<ExtensionConfigViewModel>())
				.ToNotifyCollectionChanged();
	}

	/// <summary>
	/// 対象拡張子
	/// </summary>
	public INotifyCollectionChangedSynchronizedViewList<ExtensionConfigViewModel> TargetExtensions {
		get;
	}

	public ReactiveCommand AddExtensionCommand {
		get;
	} = new();
}
