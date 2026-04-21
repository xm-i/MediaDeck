using MediaDeck.Common.Base;
using MediaDeck.Common.Extensions;
using MediaDeck.Composition.Stores.Config.Model;

namespace MediaDeck.ViewModels.Preferences.Config;

[Inject(InjectServiceLifetime.Transient)]
public class SearchConfigPageViewModel : ViewModelBase, IConfigPageViewModel {
	/// <summary>
	/// ページ名
	/// </summary>
	public string PageName {
		get;
	} = "検索";

	/// <summary>
	/// ページのアイコン（Segoe Fluent Icons のグリフ文字）
	/// </summary>
	public string PageIconGlyph {
		get;
	} = "\uE71E";

	/// <summary>
	/// ページの説明
	/// </summary>
	public string PageDescription {
		get;
	} = "検索やデータ読み込みパフォーマンスに関する設定です";


	public SearchConfigPageViewModel(SearchConfigModel searchConfig) {
		this.InitialLoadCount = searchConfig.InitialLoadCount.ToTwoWayBindableReactiveProperty(500, this.CompositeDisposable).AddTo(this.CompositeDisposable);
		this.IncrementalLoadCount = searchConfig.IncrementalLoadCount.ToTwoWayBindableReactiveProperty(10000, this.CompositeDisposable).AddTo(this.CompositeDisposable);
		this.MaxLoadCount = searchConfig.MaxLoadCount.ToTwoWayBindableReactiveProperty(50000, this.CompositeDisposable).AddTo(this.CompositeDisposable);
	}

	/// <summary>
	/// 初期ロード件数
	/// </summary>
	public BindableReactiveProperty<int> InitialLoadCount {
		get;
	}

	/// <summary>
	/// 増分読み込み件数
	/// </summary>
	public BindableReactiveProperty<int> IncrementalLoadCount {
		get;
	}

	/// <summary>
	/// 最大件数
	/// </summary>
	public BindableReactiveProperty<int> MaxLoadCount {
		get;
	}
}