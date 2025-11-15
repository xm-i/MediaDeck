using Microsoft.UI.Xaml.Controls;
using MediaDeck.ViewModels.Preferences.Config;
using Microsoft.UI.Xaml.Navigation;
using MediaDeck.Composition.Enum;

namespace MediaDeck.Views.Preferences.Config;
public sealed partial class ScanConfigPage : Page {
	public ScanConfigPage() {
		this.InitializeComponent();
	}

	/// <summary>
	/// ナビゲート時に ViewModel を受け取ります。
	/// </summary>
	protected override void OnNavigatedTo(NavigationEventArgs e) {
		if (e.Parameter is not ScanConfigPageViewModel vm) {
			throw new InvalidOperationException("ViewModel is not passed.");
		}
		this.ViewModel = vm;
		base.OnNavigatedTo(e);
	}

	public ScanConfigPageViewModel? ViewModel {
		get;
		set;
	}

	public MediaType[] MediaTypeConditions {
		get;
	} = Enum.GetValues<MediaType>();

}