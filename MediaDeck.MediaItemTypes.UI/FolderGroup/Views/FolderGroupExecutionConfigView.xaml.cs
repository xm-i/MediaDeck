using MediaDeck.Composition.Interfaces.MediaItemTypes.Views;
using MediaDeck.MediaItemTypes.FolderGroup.ViewModels;
using MediaDeck.MediaItemTypes.UI.Base.Views;

namespace MediaDeck.MediaItemTypes.UI.FolderGroup.Views;

public sealed partial class FolderGroupExecutionConfigView : IExecutionConfigView {
	public FolderGroupExecutionConfigView() {
		this.InitializeComponent();
	}
}

public class FolderGroupExecutionConfigViewUserControl : UserControlBase<FolderGroupExecutionProgramConfigViewModel>;