using System.IO;
using System.Reflection;

using CommunityToolkit.Mvvm.DependencyInjection;

using FFMpegCore;

using MediaDeck.Composition.Constants;
using MediaDeck.Database;
using MediaDeck.FileTypes.Base;
using MediaDeck.Stores.Config;
using MediaDeck.Stores.State;
using MediaDeck.Views;

using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
namespace MediaDeck;

public partial class App : Application {
	private Window? _window;
	private readonly ConfigStore _configStore;
	private readonly StateStore _stateStore;

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
		this._window.Closed += (_, _) => {
			this._stateStore.Save();
			this._configStore.Save();
			Current.Exit();
		};
		this._window.Activate();
	}

	private static void BuildConfigureServices() {
		var serviceCollection = new ServiceCollection();

		serviceCollection.AddGeneratedServices();
		MediaDeck.Composition.DIRegistration.AddGeneratedServices(serviceCollection);

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
