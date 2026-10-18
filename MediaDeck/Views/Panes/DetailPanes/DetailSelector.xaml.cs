using MediaDeck.ViewModels.Panes.DetailPanes;

namespace MediaDeck.Views.Panes.DetailPanes;

/// <summary>
/// 詳細情報ペインのセレクター。
/// </summary>
public sealed partial class DetailSelector {
	/// <summary>
	/// <see cref="DetailSelector"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	public DetailSelector() {
		this.InitializeComponent();
	}

	/// <inheritdoc/>
	protected override void OnViewModelChanged(DetailSelectorViewModel? oldViewModel, DetailSelectorViewModel? newViewModel) {
		base.OnViewModelChanged(oldViewModel, newViewModel);
		this.ViewModel?.LoadTagCandidatesCommand.Execute(Unit.Default);
	}
}

/// <summary>
/// DetailSelector の基底クラス。UserControlBase を介して ViewModel プロパティを提供します。
/// </summary>
public abstract class DetailSelectorUserControl : UserControlBase<DetailSelectorViewModel>;