using MediaDeck.Composition.Interfaces.MediaItemTypes.Views;
using MediaDeck.MediaItemTypes.Base.ViewModels;

namespace MediaDeck.MediaItemTypes.Base.Views;

internal sealed partial class DefaultExecutionConfigView : IExecutionConfigView {
	public DefaultExecutionConfigView() {
		this.InitializeComponent();
	}

}

internal class DefaultExecutionConfigViewUserControl : UserControlBase<DefaultExecutionProgramConfigViewModel>;