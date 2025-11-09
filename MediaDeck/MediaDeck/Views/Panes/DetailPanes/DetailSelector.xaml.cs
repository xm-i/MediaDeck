using MediaDeck.Composition.Bases;
using MediaDeck.ViewModels.Panes.DetailPanes;

namespace MediaDeck.Views.Panes.DetailPanes;

public sealed partial class DetailSelector : DetailSelectorUserControl {
    public DetailSelector() {
        this.InitializeComponent();
	}

	protected override void OnViewModelChanged(DetailSelectorViewModel? oldViewModel, DetailSelectorViewModel? newViewModel) {
		this.ViewModel?.LoadTagCandidatesCommand.Execute(Unit.Default);
	}
}

public abstract class DetailSelectorUserControl : UserControlBase<DetailSelectorViewModel>;
