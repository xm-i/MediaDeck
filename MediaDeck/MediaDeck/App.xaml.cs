using Microsoft.UI.Xaml;

using MediaDeck.Views;

using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using Microsoft.Data.Sqlite;
using MediaDeck.Database;
using System.IO;
using MediaDeck.Models.Preferences;
using FFMpegCore;
using MediaDeck.FileTypes.Base;
using MediaDeck.Utils.Constants;
using System.Diagnostics;
namespace MediaDeck;

public partial class App : Application {
	private Window? _window;
	private readonly string _stateFilePath;
	private readonly string _configFilePath;
	private readonly Config _config;
	private readonly States _states;

	public App() {
		if (!Directory.Exists(FilePathConstants.BaseDirectory)) {
			Directory.CreateDirectory(FilePathConstants.BaseDirectory);
		}
		this._stateFilePath = Path.Combine(FilePathConstants.BaseDirectory, "MediaDeck.states");
		this._configFilePath = Path.Combine(FilePathConstants.BaseDirectory, "MediaDeck.config");
		var serviceCollection = new ServiceCollection();

		var targetTypes = Assembly 
			.GetExecutingAssembly()
			.GetTypes()
			.Where(x =>
				x.GetCustomAttributes<AddTransientAttribute>(inherit: true).Any());

		foreach (var targetType in targetTypes) {
			var attribute = targetType.GetCustomAttribute<AddTransientAttribute>();
			serviceCollection.AddTransient(attribute?.ServiceType ?? targetType, targetType);
		}

		var singletonTargetTypes = Assembly
			.GetExecutingAssembly()
			.GetTypes()
			.Where(x =>
				x.GetCustomAttributes<AddSingletonAttribute>(inherit: true).Any());

		foreach (var singletonTargetType in singletonTargetTypes) {
			serviceCollection.AddSingleton(singletonTargetType);
		}

		var fileTypes =
			Assembly
				.GetExecutingAssembly()
				.GetTypes()
				.Where(x =>
					x.GetInterfaces()
					.Any(t => t == typeof(IFileType)))
				.Where(x => x.IsAbstract == false);
		foreach (var fileTypeType in fileTypes) {
			serviceCollection.AddSingleton(typeof(IFileType), fileTypeType);
		}

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
		var db = Ioc.Default.GetRequiredService<MediaDeckDbContext>();
		db.Database.EnsureCreated();
		InitialDataRegisterer.Register(db);

		this._config = Ioc.Default.GetRequiredService<Config>();
		this._config.SetFilePath(this._configFilePath);
		this._config.Load();
		this._states = Ioc.Default.GetRequiredService<States>();
		this._states.SetFilePath(this._stateFilePath);
		this._states.Load();
		Directory.CreateDirectory(this._config.PathConfig.TemporaryFolderPath.Value);

		Reactive.Bindings.UIDispatcherScheduler.Initialize();

		GlobalFFOptions.Configure(options => {
			options.BinaryFolder = Path.Combine(this._config.PathConfig.FFMpegFolderPath.Value);
		});
		this.InitializeComponent();
	}

	/// <summary>
	/// Invoked when the application is launched.
	/// </summary>
	/// <param name="args">Details about the launch request and process.</param>
	protected override void OnLaunched(LaunchActivatedEventArgs args) {
		this._window = Ioc.Default.GetRequiredService<MainWindow>();
		this._window.Closed += (_, _) => {
			this._states.Save();
			this._config.Save();
			Current.Exit();
		};
		this._window.Activate();
	}
}
