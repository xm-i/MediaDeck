using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.DependencyInjection;
using FFMpegCore;
using MapControl;
using MediaDeck.Composition.Constants;
using MediaDeck.Composition.Database;
using MediaDeck.Composition.Interfaces;
using MediaDeck.Composition.Interfaces.Services;
using MediaDeck.Composition.Interfaces.Tags;
using MediaDeck.Composition.Stores.State.Model;
using MediaDeck.Core.Models.Tools;
using MediaDeck.Core.Stores.Config;
using MediaDeck.Core.Stores.State;
using MediaDeck.Services;
using MediaDeck.ViewModels;
using MediaDeck.Views.FolderManager;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using Serilog;

namespace MediaDeck;

/// <summary>
/// アプリケーションのエントリー動作とDI・ライフサイクルを管理するクラス。
/// </summary>
public partial class App {
	private readonly IConfigStore _configStore;
	private readonly IStateStore _stateStore;
	private readonly ILogger<App> _logger;
	private DispatcherQueue? _dispatcherQueue;
	private IMediaEngineService? _mediaEngineService;

	/// <summary>
	/// <see cref="App"/> クラスの新しいインスタンスを初期化し、DI・ロガーおよび未処理例外ハンドラを構築する。
	/// </summary>
	public App() {
		if (!Directory.Exists(FilePathConstants.BaseDirectory)) {
			Directory.CreateDirectory(FilePathConstants.BaseDirectory);
		}
		BuildConfigureServices();
		this._logger = Ioc.Default.GetRequiredService<ILogger<App>>();
		this.RegisterUnhandledExceptionHandlers();

		this._configStore = Ioc.Default.GetRequiredService<IConfigStore>();
		this._stateStore = Ioc.Default.GetRequiredService<IStateStore>();

		// 言語設定を UI 初期化よりも前に適用する
		var language = this._configStore.Config.LanguageConfig.Language.Value;
#if UNPACKAGED
		Microsoft.Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride = string.IsNullOrEmpty(language) ? "en" : language;
#else
		Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride = string.IsNullOrEmpty(language) ? string.Empty : language;
#endif
		this.InitializeComponent();
		AppDomain.CurrentDomain.ProcessExit += (_, _) => {
			this._mediaEngineService?.Shutdown();
			this._stateStore.Save();
			this._configStore.Save();
			this.Vacuum();
		};
	}

	/// <summary>
	/// アプリケーション起動時の初期化とメインウィンドウ復元を実行する。
	/// </summary>
	/// <param name="args">起動リクエストとプロセスに関する詳細情報。</param>
	protected override async void OnLaunched(LaunchActivatedEventArgs args) {
		this._dispatcherQueue = DispatcherQueue.GetForCurrentThread();
		this._mediaEngineService = Ioc.Default.GetRequiredService<IMediaEngineService>();

		// スプラッシュ画面を表示
		var stringProvider = Ioc.Default.GetRequiredService<IStringProvider>();
		var splashScreen = new Views.SplashScreenWindow(this._configStore, stringProvider);
		splashScreen.Activate();

		await this.InitializeAsync(splashScreen.ViewModel);

		var windowManager = Ioc.Default.GetRequiredService<WindowManager>();
		windowManager.RestoreWindows();

		// 登録フォルダ数が0件の場合はフォルダ管理画面を自動で開く
		if (this._configStore.Config.FolderManagerConfig.Folders.Count == 0) {
			var windowService = Ioc.Default.GetRequiredService<IWindowService>();
			var mainWindow = windowManager.Windows.Count > 0 ? windowManager.Windows[0].Window : null;
			windowService.ActivateCentered<FolderManagerWindow>(mainWindow);
		}

		// メインウィンドウが表示されたらスプラッシュ画面を閉じる
		splashScreen.Close();

		// Unpackaged版で起動時アップデート確認が有効な場合はバックグラウンドで確認を開始する
		var appEnvironmentService = Ioc.Default.GetRequiredService<IAppEnvironmentService>();
		if (appEnvironmentService.IsUnpackaged && this._configStore.Config.EnvironmentConfig.CheckForUpdatesOnStartup.Value) {
			var appUpdateService = Ioc.Default.GetRequiredService<IAppUpdateService>();
			_ = CheckForUpdatesOnStartupAsync(appUpdateService, this._logger);
		}
	}

	/// <summary>
	/// アプリケーション全体の未処理例外ハンドラを登録する。
	/// </summary>
	private void RegisterUnhandledExceptionHandlers() {
		AppDomain.CurrentDomain.UnhandledException += (_, e) => {
			if (e.ExceptionObject is Exception exception) {
				this._logger.LogError(exception, "UnhandledException (IsTerminating={IsTerminating})", e.IsTerminating);
			} else {
				this._logger.LogError("UnhandledException (IsTerminating={IsTerminating}): {ExceptionObject}", e.IsTerminating, e.ExceptionObject);
			}
			if (e.IsTerminating) {
				Log.CloseAndFlush();
			}
		};
		TaskScheduler.UnobservedTaskException += (_, e) => {
			this._logger.LogError(e.Exception, "TaskScheduler UnobservedTaskException");
		};
		this.UnhandledException += (_, e) => {
			this._logger.LogError(e.Exception, "XAML UnhandledException: {Message}", e.Message);
		};
		ObservableSystem.RegisterUnhandledExceptionHandler(e => {
			this._logger.LogError(e, "R3 UnhandledException");
		});
	}

	/// <summary>
	/// アプリケーション起動時にバックグラウンドで新バージョンの確認を行います。
	/// </summary>
	/// <param name="appUpdateService">アプリケーション更新サービス</param>
	/// <param name="logger">ロガー</param>
	private static async Task CheckForUpdatesOnStartupAsync(IAppUpdateService appUpdateService, Microsoft.Extensions.Logging.ILogger logger) {
		try {
			await appUpdateService.CheckForUpdatesAsync();
		} catch (Exception ex) {
			logger.LogWarning(ex, "起動時のアップデート確認中に予期しないエラーが発生しました。");
		}
	}

	/// <summary>
	/// 他プロセスからのリダイレクトアクティベーションを処理する。
	/// バックグラウンドスレッドから呼ばれるため、UIスレッドにディスパッチする。
	/// </summary>
	public void HandleRedirectedActivation(AppActivationArguments args) {
		this._dispatcherQueue?.TryEnqueue(() => {
			this._stateStore.RootState.Windows.Add(Ioc.Default.GetRequiredService<WindowStateModel>());
		});
	}

	/// <summary>
	/// データベースの最適化（VACUUM）を実行する。
	/// </summary>
	private void Vacuum() {
		try {
			var dbFactory = Ioc.Default.GetRequiredService<IDbContextFactory<MediaDeckDbContext>>();
			using var db = dbFactory.CreateDbContext();
			db.Database.ExecuteSqlRaw("VACUUM");
		} catch (Exception ex) {
			Log.Error(ex, "データベースの最適化（VACUUM）中にエラーが発生しました。");
		}
	}

	private static void BuildConfigureServices() {
		// Serilog設定
		string[] logFields = [
			"{Timestamp:yyyy-MM-dd HH:mm:ss.fff}",
			"{Level:u4}",
			"{ThreadId:00}",
			"{Message:j}",
			"{SourceContext}",
			"{NewLine}{Exception}"
		];

		Log.Logger = new LoggerConfiguration()
			.Enrich.WithThreadId()
#if DEBUG
			.MinimumLevel.Verbose()
			.MinimumLevel.Override("Microsoft.EntityFrameworkCore", Serilog.Events.LogEventLevel.Information)
#else
			.MinimumLevel.Information()
#endif
			.WriteTo.Debug(outputTemplate: string.Join("｜", logFields))
			.WriteTo.File(Path.Combine(FilePathConstants.BaseDirectory, "log", ".log"),
				rollingInterval: RollingInterval.Month,
				outputTemplate: string.Join("\t", logFields))
			.CreateLogger();

		var serviceCollection = new ServiceCollection();
		serviceCollection.AddLogging(loggingBuilder => {
			loggingBuilder.AddSerilog(dispose: true);
		});

		serviceCollection.AddGeneratedServices();
		Composition.DIRegistration.AddGeneratedServices(serviceCollection);
		MediaItemTypes.DIRegistration.AddGeneratedServices(serviceCollection);
		MediaItemTypes.UI.DIRegistration.AddGeneratedServices(serviceCollection);
		ViewModels.DIRegistration.AddGeneratedServices(serviceCollection);
		Core.DIRegistration.AddGeneratedServices(serviceCollection);
		Store.DIRegistration.AddGeneratedServices(serviceCollection);

		// DataBase
		var sb = new SqliteConnectionStringBuilder { DataSource = FilePathConstants.DatabaseFilePath };
		serviceCollection.AddDbContextFactory<MediaDeckDbContext>(x => {
			x.UseSqlite(sb.ConnectionString);
		},
			ServiceLifetime.Transient);

		Ioc.Default.ConfigureServices(serviceCollection.BuildServiceProvider());
	}

	private async Task InitializeAsync(SplashScreenViewModel? splashViewModel = null) {

		await Task.Run(async () => {
			var stringProvider = Ioc.Default.GetRequiredService<IStringProvider>();
			splashViewModel?.UpdateStatus(stringProvider.GetString("Splash_CleaningUpStates"));

			// OpenStreetMapのタイルサーバーにアクセスする際のUser-Agentを設定
			var version = Assembly.GetEntryAssembly()?.GetName().Version;
			var versionString = version != null ? $"{version.Major}.{version.Minor}.{version.Build}" : "1.0.0";
			ImageLoader.HttpClient.DefaultRequestHeaders.Add("User-Agent", $"MediaDeck/{versionString} (+https://github.com/xm-i/MediaDeck)");

			// 画像メタデータ取得にSJISが必要
			Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);


			splashViewModel?.UpdateStatus(stringProvider.GetString("Splash_InitializingDatabase"));
			var databaseMigrationService = Ioc.Default.GetRequiredService<IDatabaseMigrationService>();
			await databaseMigrationService.InitializeAsync(FilePathConstants.DatabaseFilePath);

			splashViewModel?.UpdateStatus(stringProvider.GetString("Splash_LoadingConfig"));
			Directory.CreateDirectory(this._configStore.Config.PathConfig.TemporaryFolderPath.Value);

			GlobalFFOptions.Configure(options => {
				options.BinaryFolder = Path.Combine(this._configStore.Config.PathConfig.FFMpegFolderPath.Value);
			});

			splashViewModel?.UpdateStatus(stringProvider.GetString("Splash_InitializingTags"));
			var tagsManager = Ioc.Default.GetRequiredService<ITagsManager>();
			await tagsManager.InitializeAsync();

			splashViewModel?.UpdateStatus(stringProvider.GetString("Splash_PreparingBackgroundTasks"));
			var backgroundTasksModel = Ioc.Default.GetRequiredService<BackgroundTasksModel>();
			backgroundTasksModel.Start();

			splashViewModel?.UpdateStatus(stringProvider.GetString("Splash_Ready"));
		});
	}

}