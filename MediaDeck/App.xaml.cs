using System.IO;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.DependencyInjection;

using FFMpegCore;

using MediaDeck.Composition.Constants;
using MediaDeck.Composition.Database;
using MediaDeck.Composition.Interfaces.Tags;
using MediaDeck.Composition.Stores.State.Model;
using MediaDeck.Core.Stores.Config;
using MediaDeck.Core.Stores.State;
using MediaDeck.Services;

using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;

using Serilog;
using Serilog.Events;

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
		this.InitializeComponent();
	}

	/// <summary>
	/// Invoked when the application is launched.
	/// </summary>
	/// <param name="args">Details about the launch request and process.</param>
	protected override async void OnLaunched(LaunchActivatedEventArgs args) {
		this._dispatcherQueue = DispatcherQueue.GetForCurrentThread();

		await this.InitializeAsync();

		var windowManager = Ioc.Default.GetRequiredService<IWindowManager>();
		windowManager.RestoreWindows();

		var logger = LoggerFactory.CreateLogger<App>();
		AppDomain.CurrentDomain.UnhandledException += (_, e) => {
			logger.LogError(e.ExceptionObject as Exception, "UnhandledException");
		};
	}

	/// <summary>
	/// 他プロセスからのリダイレクトアクティベーションを処理する。
	/// バックグラウンドスレッドから呼ばれるため、UIスレッドにディスパッチする。
	/// </summary>
	public void HandleRedirectedActivation(AppActivationArguments args) {
		this._dispatcherQueue?.TryEnqueue(() => {
			this._stateStore.RootState.Windows.Add(new WindowStateModel());
		});
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
			.MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Information)
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
		var sb = new SqliteConnectionStringBuilder { DataSource = Path.Combine(FilePathConstants.BaseDirectory, "pix.db") };
		serviceCollection.AddDbContextFactory<MediaDeckDbContext>(x => {
			x.UseSqlite(sb.ConnectionString);
		},
			ServiceLifetime.Transient);

		Ioc.Default.ConfigureServices(serviceCollection.BuildServiceProvider());
	}

	private async Task InitializeAsync() {

		var dbFactory = Ioc.Default.GetRequiredService<IDbContextFactory<MediaDeckDbContext>>();
		using (var db = dbFactory.CreateDbContext()) {
			db.Database.EnsureCreated();
		}

		Directory.CreateDirectory(this._configStore.Config.PathConfig.TemporaryFolderPath.Value);

		GlobalFFOptions.Configure(options => {
			options.BinaryFolder = Path.Combine(this._configStore.Config.PathConfig.FFMpegFolderPath.Value);
		});

		var tagsManager = Ioc.Default.GetRequiredService<ITagsManager>();
		await tagsManager.InitializeAsync();


		FlyleafLib.Engine.Start(new FlyleafLib.EngineConfig() {
#if DEBUG
			LogOutput = ":debug",
			LogLevel = FlyleafLib.LogLevel.Debug,
			FFmpegLogLevel = Flyleaf.FFmpeg.LogLevel.Warn,
#endif
			UIRefresh = false,
			FFmpegPath = this._configStore.Config.PathConfig.FFMpegFolderPath.Value,
		});
	}
}