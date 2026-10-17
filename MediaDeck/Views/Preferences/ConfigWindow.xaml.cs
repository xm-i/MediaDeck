using MediaDeck.Composition.Interfaces;
using MediaDeck.Core.Stores.Config;
using MediaDeck.ViewModels.Preferences;
using MediaDeck.ViewModels.Preferences.Config;
using MediaDeck.Views.Helpers;
using MediaDeck.Views.Preferences.Config;
using Microsoft.UI.Xaml;

namespace MediaDeck.Views.Preferences;

[Inject(InjectServiceLifetime.Transient)]
public sealed partial class ConfigWindow : Window {
	private readonly CompositeDisposable _disposable = new();

	public ConfigWindowViewModel ViewModel {
		get;
	}

	public ConfigWindow(ConfigWindowViewModel configWindowViewModel, IConfigStore configStore, IStringProvider stringProvider) {
		this.InitializeComponent();
		this.ViewModel = configWindowViewModel;

		this.Title = stringProvider.GetString("ConfigWindow_Title");

		// テーマのバインド
		ThemeHelper.BindTheme(this, configStore, this._disposable);

		this.AppWindow.Resize(new(1000, 700));

		this.ViewModel.SelectedPageViewModel.Subscribe(vm => {
			var view = vm switch {
				ScanConfigPageViewModel => typeof(ScanConfigPage),
				ThumbnailConfigPageViewModel => typeof(ThumbnailConfigPage),
				ExecutionConfigPageViewModel => typeof(ExecutionConfigPage),
				SearchConfigPageViewModel => typeof(SearchConfigPage),
				LanguageConfigPageViewModel => typeof(LanguageConfigPage),
				EnvironmentConfigPageViewModel => typeof(EnvironmentConfigPage),
				_ => null
			};

			if (view != null) {
				this.ContentFrame.Navigate(view, vm);
			}
		}).AddTo(this._disposable);

		this.ViewModel.RequestClose.Subscribe(_ => {
			this.Close();
		}).AddTo(this._disposable);

		this.Closed += (s, e) => this._disposable.Dispose();
	}

	private void Window_Loaded(object sender, RoutedEventArgs e) {
		this.ExtendsContentIntoTitleBar = true;
		this.SetTitleBar(this.AppTitleBar);
	}
}