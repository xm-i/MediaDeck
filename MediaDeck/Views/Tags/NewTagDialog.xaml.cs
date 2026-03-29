using MediaDeck.ViewModels.Tags;

namespace MediaDeck.Views.Tags;

public sealed partial class NewTagDialog {
	public NewTagDialog(NewTagDialogViewModel viewModel) {
		this.ViewModel = viewModel;
		this.InitializeComponent();

		this.PrimaryButtonClick += (_, _) => {
			this.ViewModel.ConfirmCommand.Execute(Unit.Default);
		};

		this.CloseButtonClick += (_, _) => {
			this.ViewModel.CancelCommand.Execute(Unit.Default);
		};
	}

	public NewTagDialogViewModel ViewModel {
		get;
	}
}