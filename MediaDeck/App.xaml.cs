using System.IO;

using CommunityToolkit.Mvvm.DependencyInjection;

using FFMpegCore;

using MediaDeck.Composition.Constants;
using MediaDeck.Composition.Interfaces;
using MediaDeck.Database;
using MediaDeck.Stores.Config;
using MediaDeck.Stores.State;
using MediaDeck.Views;

using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;

using Serilog;
using Serilog.Events;
namespace MediaDeck;

public partial class App : Application {
	private Window? _window;
	private readonly ConfigStore _configStore;
	private readonly StateStore _stateStore;


	/// <summary>
	///     ILoggerFactory for DI外クラスでのログ使用。
	/// </summary>
	public static ILoggerFactory LoggerFactory {
		get {
			return field ??= Ioc.Default.GetRequiredService<ILoggerFactory>();
		}
	}

	public App() {
		if (!Directory.Exists(FilePathConstants.BaseDirectory)) {
			Directory.CreateDirectory(FilePathConstants.BaseDirectory);
		}
		BuildConfigureServices();

		var db = Ioc.Default.GetRequiredService<MediaDeckDbContext>();
		db.Database.EnsureCreated();
		InitialDataRegisterer.Register(db);

		this._configStore = Ioc.Default.GetRequiredService<ConfigStore>();
		this._stateStore = Ioc.Default.GetRequiredService<StateStore>();
		Directory.CreateDirectory(this._configStore.Config.PathConfig.TemporaryFolderPath.Value);

		GlobalFFOptions.Configure(options => {
			options.BinaryFolder = Path.Combine(this._configStore.Config.PathConfig.FFMpegFolderPath.Value);
		});
		this.InitializeComponent();
	}

	/// <summary>
	/// Invoked when the application is launched.
	/// </summary>
	/// <param name="args">Details about the launch request and process.</param>
	protected override void OnLaunched(LaunchActivatedEventArgs args) {
		this._window = Ioc.Default.GetRequiredService<MainWindow>();
		Ioc.Default.GetRequiredService<IDispatcherGate>().Initialize(this._window.DispatcherQueue);

		this._window.Closed += (_, _) => {
			this._stateStore.Save();
			this._configStore.Save();
			Current.Exit();
		};
		var logger = LoggerFactory.CreateLogger<App>();
		AppDomain.CurrentDomain.UnhandledException += (s, e) => {
			logger.LogError(e.ExceptionObject as Exception, "UnhandledException");
		};
		this._window.Activate();
	}

	private static void BuildConfigureServices() {
		// Serilog設定
		string[] logFields = [
			"{Timestamp:HH:mm:ss.fff}",
			"{Level:u4}",
			"{ThreadId:00}",
			"{Message:j}",
			"{SourceContext}",
			"{NewLine}{Exception}"
		];

		Log.Logger = new LoggerConfiguration()
			.Enrich.WithThreadId()
#if DEBUG || DEBUG_UNPACKAGED
			.MinimumLevel.Verbose()
			.MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Information)
#else
			.MinimumLevel.Information()
#endif
			.WriteTo.Debug(outputTemplate: string.Join("｜", logFields))
			.WriteTo.File(
				Path.Combine(FilePathConstants.BaseDirectory, "log", ".log"),
				rollingInterval: RollingInterval.Month,
				outputTemplate: string.Join("\t", logFields))
			.CreateLogger();

		var serviceCollection = new ServiceCollection();
		serviceCollection.AddLogging(loggingBuilder => {
			loggingBuilder.AddSerilog(dispose: true);
		});

		serviceCollection.AddGeneratedServices();
		Composition.DIRegistration.AddGeneratedServices(serviceCollection);

		// DataBase
		var sb = new SqliteConnectionStringBuilder {
			DataSource = Path.Combine(FilePathConstants.BaseDirectory, "pix.db")
		};
		serviceCollection.AddDbContextFactory<MediaDeckDbContext>(x => {
			x.UseSqlite(sb.ConnectionString);
		}, ServiceLifetime.Transient);

		Ioc.Default.ConfigureServices(
			serviceCollection.BuildServiceProvider()
		);
	}
}
