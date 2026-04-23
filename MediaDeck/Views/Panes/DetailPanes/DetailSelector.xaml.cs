using MediaDeck.ViewModels.Panes.DetailPanes;

namespace MediaDeck.Views.Panes.DetailPanes;

public sealed partial class DetailSelector {
	public DetailSelector() {
		this.InitializeComponent();
		this.Loaded += (_, _) => {
			this.Bindings.Update();
		};
	}

	protected override void OnViewModelChanged(DetailSelectorViewModel? oldViewModel, DetailSelectorViewModel? newViewModel) {
		this.ViewModel?.LoadTagCandidatesCommand.Execute(Unit.Default);
	}
}

public abstract class DetailSelectorUserControl : UserControlBase<DetailSelectorViewModel>;