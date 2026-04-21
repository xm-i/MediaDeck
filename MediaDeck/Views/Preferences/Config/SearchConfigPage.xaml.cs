using MediaDeck.ViewModels.Preferences.Config;

using Microsoft.UI.Xaml.Navigation;

namespace MediaDeck.Views.Preferences.Config;

public sealed partial class SearchConfigPage {
	public SearchConfigPage() {
		this.InitializeComponent();
	}

	/// <summary>
	/// ナビゲート時に ViewModel を受け取り、DataContext にも設定します。
	/// </summary>
	protected override void OnNavigatedTo(NavigationEventArgs e) {
		if (e.Parameter is not SearchConfigPageViewModel vm) {
			throw new InvalidOperationException("ViewModel is not passed.");
		}
		this.ViewModel = vm;
		this.DataContext = vm;
		base.OnNavigatedTo(e);
	}

	/// <summary>
	/// ビューモデル
	/// </summary>
	public SearchConfigPageViewModel? ViewModel {
		get;
		set;
	}
}