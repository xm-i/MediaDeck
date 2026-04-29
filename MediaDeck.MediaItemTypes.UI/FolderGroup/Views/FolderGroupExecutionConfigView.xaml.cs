using MediaDeck.MediaItemTypes.FolderGroup.ViewModels;
using MediaDeck.MediaItemTypes.UI.Base.Views;

namespace MediaDeck.MediaItemTypes.UI.FolderGroup.Views;

public sealed partial class FolderGroupExecutionConfigView {
	public FolderGroupExecutionConfigView() {
		this.InitializeComponent();
	}
}

public class FolderGroupExecutionConfigViewUserControl : UserControlBase<FolderGroupExecutionProgramConfigViewModel>;