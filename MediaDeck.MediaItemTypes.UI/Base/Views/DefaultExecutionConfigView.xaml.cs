using MediaDeck.Composition.Interfaces.MediaItemTypes.Views;
using MediaDeck.MediaItemTypes.Base.ViewModels;

namespace MediaDeck.MediaItemTypes.UI.Base.Views;

public sealed partial class DefaultExecutionConfigView : IExecutionConfigView {
	public DefaultExecutionConfigView() {
		this.InitializeComponent();
	}

}

public class DefaultExecutionConfigViewUserControl : UserControlBase<DefaultExecutionProgramConfigViewModel>;