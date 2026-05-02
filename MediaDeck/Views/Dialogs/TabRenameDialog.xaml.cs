using MediaDeck.Core.Stores.State;
using MediaDeck.Views.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace MediaDeck.Views.Dialogs;

[Inject(InjectServiceLifetime.Transient)]
public sealed partial class TabRenameDialog : ContentDialog {
	public string ResultText {
		get {
			return this.NameTextBox.Text;
		}
	}

	private readonly CompositeDisposable _disposable = new();

	public TabRenameDialog(IStateStore stateStore) {
		this.InitializeComponent();

		ThemeHelper.BindTheme(this, stateStore, this._disposable);
		this.Closed += (_, _) => this._disposable.Dispose();
	}

	public void Initialize(string initialName) {
		this.NameTextBox.Text = initialName;
	}

	private void NameTextBox_Loaded(object sender, RoutedEventArgs e) {
		this.NameTextBox.Focus(FocusState.Programmatic);
		this.NameTextBox.SelectAll();
	}
}