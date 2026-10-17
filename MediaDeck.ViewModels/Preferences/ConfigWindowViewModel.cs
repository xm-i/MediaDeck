using MediaDeck.Common.Base;
using MediaDeck.Core.Stores.Config;
using MediaDeck.ViewModels.Preferences.Config;

namespace MediaDeck.ViewModels.Preferences;

[Inject(InjectServiceLifetime.Transient)]
public class ConfigWindowViewModel : ViewModelBase {
	public ConfigWindowViewModel(IConfigStore configStore, ScanConfigPageViewModel scanConfigPageViewModel, ThumbnailConfigPageViewModel thumbnailConfigPageViewModel, ExecutionConfigPageViewModel executionConfigPageViewModel, SearchConfigPageViewModel searchConfigPageViewModel, LanguageConfigPageViewModel languageConfigPageViewModel, EnvironmentConfigPageViewModel environmentConfigPageViewModel) {
		this.ScanConfigPageViewModel = scanConfigPageViewModel;
		this.ThumbnailConfigPageViewModel = thumbnailConfigPageViewModel;
		this.ExecutionConfigPageViewModel = executionConfigPageViewModel;
		this.SearchConfigPageViewModel = searchConfigPageViewModel;
		this.LanguageConfigPageViewModel = languageConfigPageViewModel;
		this.EnvironmentConfigPageViewModel = environmentConfigPageViewModel;
		this.ConfigPageViewModels = [
			this.ScanConfigPageViewModel,
			this.ThumbnailConfigPageViewModel,
			this.ExecutionConfigPageViewModel,
			this.SearchConfigPageViewModel,
			this.LanguageConfigPageViewModel,
			this.EnvironmentConfigPageViewModel,
		];
		this.SelectedPageViewModel.Value = this.ScanConfigPageViewModel;
		this.SaveCommand.Subscribe(_ => {
			configStore.Save();
			this.Close();
		}).AddTo(this.CompositeDisposable);

		this.ApplyCommand.Subscribe(_ => {
			configStore.Save();
		}).AddTo(this.CompositeDisposable);
	}

	/// <summary>
	/// 設定ページのViewModel一覧
	/// </summary>
	public IConfigPageViewModel[] ConfigPageViewModels {
		get;
	}

	/// <summary>
	/// 選択中の設定ページViewModel
	/// </summary>
	public BindableReactiveProperty<IConfigPageViewModel> SelectedPageViewModel {
		get;
	} = new();

	/// <summary>
	/// 保存してウィンドウを閉じるコマンド
	/// </summary>
	public ReactiveCommand SaveCommand {
		get;
	} = new();

	/// <summary>
	/// 設定を保存して適用するコマンド
	/// </summary>
	public ReactiveCommand ApplyCommand {
		get;
	} = new();

	/// <summary>
	/// スキャン設定ページのViewModel
	/// </summary>
	public ScanConfigPageViewModel ScanConfigPageViewModel {
		get;
	}

	/// <summary>
	/// サムネイル設定ページのViewModel
	/// </summary>
	public ThumbnailConfigPageViewModel ThumbnailConfigPageViewModel {
		get;
	}

	/// <summary>
	/// 実行プログラム設定ページのViewModel
	/// </summary>
	public ExecutionConfigPageViewModel ExecutionConfigPageViewModel {
		get;
	}

	/// <summary>
	/// 検索設定ページのViewModel
	/// </summary>
	public SearchConfigPageViewModel SearchConfigPageViewModel {
		get;
	}

	/// <summary>
	/// 言語設定ページのViewModel
	/// </summary>
	public LanguageConfigPageViewModel LanguageConfigPageViewModel {
		get;
	}

	/// <summary>
	/// 環境設定ページのViewModel
	/// </summary>
	public EnvironmentConfigPageViewModel EnvironmentConfigPageViewModel {
		get;
	}
}