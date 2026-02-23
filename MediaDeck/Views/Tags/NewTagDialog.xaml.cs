using MediaDeck.ViewModels.Tags;
using Microsoft.UI.Xaml.Controls;
using R3;
using System;

namespace MediaDeck.Views.Tags;

public sealed partial class NewTagDialog : ContentDialog {
	public NewTagDialog(NewTagDialogViewModel viewModel) {
		this.ViewModel = viewModel;
		this.InitializeComponent();
		
		this.PrimaryButtonClick += (s, e) => {
			this.ViewModel.ConfirmCommand.Execute(Unit.Default);
		};
		
		this.CloseButtonClick += (s, e) => {
			this.ViewModel.CancelCommand.Execute(Unit.Default);
		};
	}

	public NewTagDialogViewModel ViewModel {
		get;
	}
}
