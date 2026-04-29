using MediaDeck.MediaItemTypes.Base.ViewModels;

namespace MediaDeck.MediaItemTypes.UI.Base.Views;

public sealed partial class DefaultExecutionConfigView {
	public DefaultExecutionConfigView() {
		this.InitializeComponent();
	}

}

public class DefaultExecutionConfigViewUserControl : UserControlBase<DefaultExecutionProgramConfigViewModel>;