using MediaDeck.Common.Base;
using MediaDeck.Composition.Stores.State.Model.Objects;

namespace MediaDeck.ViewModels.Panes.SortPanes;

/// <summary>
/// ソート条件ViewModel
/// </summary>
public class SortConditionViewModel : ViewModelBase {
	/// <summary>
	/// モデル
	/// </summary>
	public SortObject Model {
		get;
	}

	/// <summary>
	/// 表示名
	/// </summary>
	public BindableReactiveProperty<string> DisplayName {
		get;
	}

	/// <summary>
	/// コンストラクタ
	/// </summary>
	/// <param name="model">ソート条件モデル</param>
	public SortConditionViewModel(SortObject model) {
		this.Model = model;
		this.DisplayName = this.Model.DisplayName.ToBindableReactiveProperty(string.Empty).AddTo(this.CompositeDisposable);
	}
}