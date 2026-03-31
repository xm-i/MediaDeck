using MediaDeck.Composition.Stores.Config.Model;
using MediaDeck.Common.Base;

using Microsoft.Extensions.DependencyInjection;

namespace MediaDeck.ViewModels.Preferences.Config;

[Inject(InjectServiceLifetime.Transient)]
public class ScanConfigPageViewModel : ViewModelBase, IConfigPageViewModel {
	/// <summary>
	/// ページ名
	/// </summary>
	public string PageName {
		get;
	} = "スキャン";

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
	} = "対象拡張子やスキャン方法を設定します";


	private readonly ScanConfigModel _scanConfig;

	public ScanConfigPageViewModel(ScanConfigModel scanConfig) {
		this._scanConfig = scanConfig;
		this.AddExtensionCommand.Subscribe(_ => {
			this._scanConfig.AddTargetExtension();
		}).AddTo(this.CompositeDisposable);
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

	/// <summary>
	/// 拡張子追加コマンド
	/// </summary>
	public ReactiveCommand AddExtensionCommand {
		get;
	} = new();
}