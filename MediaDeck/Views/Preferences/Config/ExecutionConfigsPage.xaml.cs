using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using MediaDeck.ViewModels.Preferences;
using MediaDeck.ViewModels.Preferences.Config;
using MediaDeck.Composition.Enum;

namespace MediaDeck.Views.Preferences.Config;
public sealed partial class ExecutionConfigPage : Page {
	public ExecutionConfigPage() {
		this.InitializeComponent();
	}

	/// <summary>
	/// ナビゲート時に ViewModel を受け取ります。
	/// </summary>
	protected override void OnNavigatedTo(NavigationEventArgs e) {
		if (e.Parameter is not ExecutionConfigPageViewModel vm) {
			throw new InvalidOperationException("ViewModel is not passed.");
		}
		this.ViewModel = vm;
		base.OnNavigatedTo(e);
	}

	public ExecutionConfigPageViewModel? ViewModel {
		get;
		set;
	}

	public MediaType[] MediaTypeConditions {
		get;
	} = Enum.GetValues<MediaType>();

}