using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace MediaDeck.Views.Dialogs;

public sealed partial class TabRenameDialog : ContentDialog {
	public string ResultText {
		get {
			return this.NameTextBox.Text;
		}
	}

	public TabRenameDialog(string initialName) {
		this.InitializeComponent();
		this.NameTextBox.Text = initialName;
	}

	private void NameTextBox_Loaded(object sender, RoutedEventArgs e) {
		this.NameTextBox.Focus(FocusState.Programmatic);
		this.NameTextBox.SelectAll();
	}
}