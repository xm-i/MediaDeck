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

public partial class App {
	private readonly IConfigStore _configStore;
	private readonly IStateStore _stateStore;
	private DispatcherQueue? _dispatcherQueue;


	/// <summary>
	///     ILoggerFactory for DI外クラスでのログ使用。
	/// </summary>
	private static ILoggerFactory LoggerFactory {
		get {
			return field ??= Ioc.Default.GetRequiredService<ILoggerFactory>();
		}
	}

	public App() {
		if (!Directory.Exists(FilePathConstants.BaseDirectory)) {
			Directory.CreateDirectory(FilePathConstants.BaseDirectory);
		}
		BuildConfigureServices();
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
			this._stateStore.Save();
			this._configStore.Save();
			this.Vacuum();
		};
	}

	/// <summary>
	/// Invoked when the application is launched.
	/// </summary>
	/// <param name="args">Details about the launch request and process.</param>
	protected override async void OnLaunched(LaunchActivatedEventArgs args) {
		this._dispatcherQueue = DispatcherQueue.GetForCurrentThread();

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
			var mainWindow = windowManager.Windows.FirstOrDefault()?.Window;
			windowService.ActivateCentered<FolderManagerWindow>(mainWindow);
		}

		var logger = LoggerFactory.CreateLogger<App>();
		AppDomain.CurrentDomain.UnhandledException += (_, e) => {
			logger.LogError(e.ExceptionObject as Exception, "UnhandledException");
		};
		UnhandledException += (_, e) => {
			logger.LogError(e.Exception, "XAML UnhandledException");
		};
		ObservableSystem.RegisterUnhandledExceptionHandler(e => {
			logger.LogError(e, "R3 UnhandledException");
		});
		// メインウィンドウが表示されたらスプラッシュ画面を閉じる
		splashScreen.Close();
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
			var dbPath = FilePathConstants.DatabaseFilePath;
			if (!File.Exists(dbPath)) {
				{
					// 事前に0バイトファイルを作らないとEnsureCreatedAsyncで死ぬ
					using var __ = File.Create(dbPath);
				}
				var dbFactory = Ioc.Default.GetRequiredService<IDbContextFactory<MediaDeckDbContext>>();
				await using (var db = await dbFactory.CreateDbContextAsync()) {
					await db.Database.EnsureCreatedAsync();

					var dbVersion = db.DbVersions.AsNoTracking().FirstOrDefault(x => x.Id == 1);
					if (dbVersion == null) {
						db.DbVersions.Add(new() {
							Id = 1,
							Version = 1,
						});
						await db.SaveChangesAsync();
					}
				}
			}

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

			splashViewModel?.UpdateStatus(stringProvider.GetString("Splash_StartingMediaEngine"));
			await Task.Delay(100);
			this._dispatcherQueue?.TryEnqueue(() => {
				FlyleafLib.Engine.Start(new FlyleafLib.EngineConfig() {
#if DEBUG
					LogOutput = ":debug",
					LogLevel = FlyleafLib.LogLevel.Debug,
					FFmpegLogLevel = Flyleaf.FFmpeg.LogLevel.Warn,
#endif
					UIRefresh = false,
					FFmpegPath = this._configStore.Config.PathConfig.FFMpegFolderPath.Value,
				});
			});
			splashViewModel?.UpdateStatus(stringProvider.GetString("Splash_Ready"));
		});
	}

}