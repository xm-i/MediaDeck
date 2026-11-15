using MediaDeck.Common.Base;
using MediaDeck.Core.Stores.Config;
using MediaDeck.ViewModels.Preferences.Config;

namespace MediaDeck.ViewModels.Preferences;

[Inject(InjectServiceLifetime.Transient)]
public class ConfigWindowViewModel : ViewModelBase {
	/// <summary>
	/// <see cref="ConfigWindowViewModel"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	/// <param name="configStore">設定ストア</param>
	/// <param name="scanConfigPageViewModel">スキャン設定ページのViewModel</param>
	/// <param name="thumbnailConfigPageViewModel">サムネイル設定ページのViewModel</param>
	/// <param name="executionConfigPageViewModel">実行プログラム設定ページのViewModel</param>
	/// <param name="searchConfigPageViewModel">検索設定ページのViewModel</param>
	/// <param name="languageConfigPageViewModel">言語設定ページのViewModel</param>
	/// <param name="environmentConfigPageViewModel">環境設定ページのViewModel</param>
	public ConfigWindowViewModel(IConfigStore configStore, ScanConfigPageViewModel scanConfigPageViewModel, ThumbnailConfigPageViewModel thumbnailConfigPageViewModel, ExecutionConfigPageViewModel executionConfigPageViewModel, SearchConfigPageViewModel searchConfigPageViewModel, LanguageConfigPageViewModel languageConfigPageViewModel, EnvironmentConfigPageViewModel environmentConfigPageViewModel) {
		this.ScanConfigPageViewModel = scanConfigPageViewModel.AddTo(this.CompositeDisposable);
		this.ThumbnailConfigPageViewModel = thumbnailConfigPageViewModel.AddTo(this.CompositeDisposable);
		this.ExecutionConfigPageViewModel = executionConfigPageViewModel.AddTo(this.CompositeDisposable);
		this.SearchConfigPageViewModel = searchConfigPageViewModel.AddTo(this.CompositeDisposable);
		this.LanguageConfigPageViewModel = languageConfigPageViewModel.AddTo(this.CompositeDisposable);
		this.EnvironmentConfigPageViewModel = environmentConfigPageViewModel.AddTo(this.CompositeDisposable);
		this.ConfigPageViewModels = [
			this.ScanConfigPageViewModel,
			this.ThumbnailConfigPageViewModel,
			this.ExecutionConfigPageViewModel,
			this.SearchConfigPageViewModel,
			this.LanguageConfigPageViewModel,
			this.EnvironmentConfigPageViewModel,
		];
		this.SelectedPageViewModel = new BindableReactiveProperty<IConfigPageViewModel>(this.ScanConfigPageViewModel).AddTo(this.CompositeDisposable);
		this.SaveCommand = new ReactiveCommand().AddTo(this.CompositeDisposable);
		this.SaveCommand.Subscribe(_ => {
			configStore.Save();
			this.Close();
		}).AddTo(this.CompositeDisposable);

		this.ApplyCommand = new ReactiveCommand().AddTo(this.CompositeDisposable);
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
	}

	/// <summary>
	/// 保存してウィンドウを閉じるコマンド
	/// </summary>
	public ReactiveCommand SaveCommand {
		get;
	}

	/// <summary>
	/// 設定を保存して適用するコマンド
	/// </summary>
	public ReactiveCommand ApplyCommand {
		get;
	}

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