using Microsoft.UI.Xaml.Controls;

using MediaDeck.Utils.Enums;
using MediaDeck.ViewModels.Preferences;
using MediaDeck.ViewModels.Preferences.CustomConfigs;

namespace MediaDeck.Views.Preferences.CustomConfig;
public sealed partial class ScanConfigPage : Page {
	public ScanConfigPage() {
		this.InitializeComponent();
		this.DataContextChanged += (s, e) => {
			if (this.DataContext is ScanConfigPageViewModel scpvm) {
				this.ViewModel = scpvm;
			} else if (this.DataContext is ConfigWindowViewModel cwvm) {
				this.ViewModel =cwvm.ScanConfigPageViewModel;
			}
		};
	}

	public ScanConfigPageViewModel? ViewModel {
		get;
		set;
	}

	public MediaType[] MediaTypeConditions {
		get;
	} = Enum.GetValues<MediaType>();

}