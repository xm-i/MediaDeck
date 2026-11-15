using MediaDeck.Common.Base;
using MediaDeck.Common.Extensions;
using MediaDeck.Composition.Interfaces;
using MediaDeck.Composition.Stores.Config.Model;

namespace MediaDeck.ViewModels.Preferences.Config;

[Inject(InjectServiceLifetime.Transient)]
public class ScanConfigPageViewModel : ViewModelBase, IConfigPageViewModel {
	/// <summary>
	/// ページ名
	/// </summary>
	public string PageName {
		get;
	}

	/// <summary>
	/// ページのアイコン（Segoe Fluent Icons のグリフ文字）
	/// </summary>
	public string PageIconGlyph {
		get;
	} = "\uE721";

	/// <summary>
	/// ページの説明
	/// </summary>
	public string PageDescription {
		get;
	}


	private readonly ScanConfigModel _scanConfig;

	public ScanConfigPageViewModel(ScanConfigModel scanConfig, IStringProvider stringProvider) {
		this._scanConfig = scanConfig;
		this.PageName = stringProvider.GetString("Config_Scan_Name");
		this.PageDescription = stringProvider.GetString("Config_Scan_Description");

		this.AddExtensionCommand.Subscribe(_ => {
			this._scanConfig.AddTargetExtension();
		})
			.AddTo(this.CompositeDisposable);
		this.TargetExtensions =
			this._scanConfig
				.TargetExtensions
				.CreateViewWithDisposal(x => new ExtensionConfigViewModel(x, scanConfig))
				.ToNotifyCollectionChangedWithDisposal()
				.AddTo(this.CompositeDisposable);
	}

	/// <summary>
	/// 対象拡張子
	/// </summary>
	public INotifyCollectionChangedSynchronizedViewList<ExtensionConfigViewModel> TargetExtensions {
		get;
	}

	/// <summary>
	/// 拡張子追加コマンド
	/// </summary>
	public ReactiveCommand AddExtensionCommand {
		get;
	} = new();
}