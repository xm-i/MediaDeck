using Microsoft.UI.Xaml.Controls;

using MediaDeck.Utils.Enums;
using MediaDeck.ViewModels.Preferences;
using MediaDeck.ViewModels.Preferences.CustomConfigs;

namespace MediaDeck.Views.Preferences.CustomConfig;
public sealed partial class ExecutionConfigPage : Page {
	public ExecutionConfigPage() {
		this.InitializeComponent();
		this.DataContextChanged += (s, e) => {
			if (this.DataContext is ExecutionConfigPageViewModel ecpvm) {
				this.ViewModel = ecpvm;
			} else if (this.DataContext is ConfigWindowViewModel cwvm) {
				this.ViewModel =cwvm.ExecutionConfigPageViewModel;
			}
		};
	}

	public ExecutionConfigPageViewModel? ViewModel {
		get;
		set;
	}

	public MediaType[] MediaTypeConditions {
		get;
	} = Enum.GetValues<MediaType>();

}