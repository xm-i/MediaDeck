using MediaDeck.ViewModels.Panes.RepositoryPanes;

namespace MediaDeck.Views.Panes.RepositoryPanes;

/// <summary>
/// リポジトリペインのセレクター。
/// </summary>
public sealed partial class RepositorySelector {
	/// <summary>
	/// <see cref="RepositorySelector"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	public RepositorySelector() {
		this.InitializeComponent();
	}

	/// <inheritdoc/>
	protected override void OnViewModelChanged(RepositorySelectorViewModel? oldViewModel, RepositorySelectorViewModel? newViewModel) {
		base.OnViewModelChanged(oldViewModel, newViewModel);
		newViewModel?.LoadCommand.Execute(Unit.Default);
	}
}

/// <summary>
/// RepositorySelector の基底クラス。UserControlBase を介して ViewModel プロパティを提供します。
/// </summary>
public abstract class RepositorySelectorUserControl : UserControlBase<RepositorySelectorViewModel>;