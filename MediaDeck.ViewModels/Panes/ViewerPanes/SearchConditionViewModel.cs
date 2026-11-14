using MediaDeck.Common.Base;
using MediaDeck.Composition.Interfaces.Files;

namespace MediaDeck.ViewModels.Panes.ViewerPanes;

/// <summary>
/// 検索条件および検索候補の ViewModel クラス。
/// </summary>
public class SearchConditionViewModel : ViewModelBase {
	/// <summary>
	/// <see cref="SearchConditionViewModel"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	/// <param name="searchCondition">検索条件モデル。</param>
	public SearchConditionViewModel(ISearchCondition searchCondition) {
		this.SearchCondition = searchCondition;
		this.DisplayText = new BindableReactiveProperty<string>(searchCondition.DisplayText).AddTo(this.CompositeDisposable);
	}

	/// <summary>
	/// 表示用テキストを取得します。
	/// </summary>
	public BindableReactiveProperty<string> DisplayText {
		get;
	}

	/// <summary>
	/// 検索条件モデルを取得します。
	/// </summary>
	public ISearchCondition SearchCondition {
		get;
	}

	/// <summary>
	/// モデルの現在状態に基づいて <see cref="DisplayText"/> を最新化します。
	/// </summary>
	public void RefreshDisplayText() {
		var latest = this.SearchCondition.DisplayText;
		if (this.DisplayText.Value != latest) {
			this.DisplayText.Value = latest;
		}
	}
}