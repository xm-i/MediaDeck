using MediaDeck.Composition.Bases;
using MediaDeck.ViewModels.Sort;

namespace MediaDeck.Views.Sort;
public sealed partial class SortManagerView: SortManagerViewUserControl {
	public SortManagerView() {
		this.InitializeComponent();
	}
}

public class SortManagerViewUserControl : UserControlBase<SortManagerViewModel> {
}