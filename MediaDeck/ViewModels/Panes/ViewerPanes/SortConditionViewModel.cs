using MediaDeck.Composition.Bases;
using MediaDeck.Composition.Stores.State.Model.Objects;

namespace MediaDeck.ViewModels.Panes.ViewerPanes;

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

	public SortConditionViewModel(SortObject model) {
		this.Model = model;
		this.DisplayName = this.Model.DisplayName.ToBindableReactiveProperty(string.Empty);
	}
}
