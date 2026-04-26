using MediaDeck.Composition.Interfaces.MediaItemTypes.Views;
using MediaDeck.MediaItemTypes.Base.Views;
using MediaDeck.MediaItemTypes.FolderGroup.ViewModels;

namespace MediaDeck.MediaItemTypes.FolderGroup.Views;

internal sealed partial class FolderGroupExecutionConfigView : IExecutionConfigView {
	public FolderGroupExecutionConfigView() {
		this.InitializeComponent();
	}
}

internal class FolderGroupExecutionConfigViewUserControl : UserControlBase<FolderGroupExecutionProgramConfigViewModel>;