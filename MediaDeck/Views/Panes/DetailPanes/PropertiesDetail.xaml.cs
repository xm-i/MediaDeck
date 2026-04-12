namespace MediaDeck.Views.Panes.DetailPanes;

public sealed partial class PropertiesDetail {
	public PropertiesDetail() {
		this.InitializeComponent();
	}

	private void RatingControl_ValueChanged(Microsoft.UI.Xaml.Controls.RatingControl sender, object args) {
		if(this.ViewModel is not { } vm)
		{
			return;
		}

		vm.UpdateRateCommand.Execute(sender.Value);
    }
}