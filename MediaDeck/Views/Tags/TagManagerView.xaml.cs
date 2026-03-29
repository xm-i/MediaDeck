using MediaDeck.ViewModels.Tags;

namespace MediaDeck.Views.Tags;

public sealed partial class TagManagerView {
	public TagManagerView() {
		this.InitializeComponent();
	}
}

public class TagManagerViewUserControl : UserControlBase<TagManagerViewModel>;