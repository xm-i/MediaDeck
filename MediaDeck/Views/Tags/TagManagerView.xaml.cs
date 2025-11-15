using MediaDeck.Composition.Bases;
using MediaDeck.ViewModels.Tags;

namespace MediaDeck.Views.Tags;
public sealed partial class TagManagerView : TagManagerViewUserControl {
	public TagManagerView() {
		this.InitializeComponent();
	}
}

public class TagManagerViewUserControl : UserControlBase<TagManagerViewModel> {
}