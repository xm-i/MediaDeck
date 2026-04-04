using System;
using System.IO;
using MediaDeck.Composition.Constants;
using MediaDeck.Composition.Stores.Config.Model;
using MediaDeck.Composition.Stores.Config.Model.Objects;
using MediaDeck.Core.Stores.Config;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Shouldly;
using Xunit;

namespace MediaDeck.Core.Tests.Stores.Config;

public class ConfigStoreTests : IDisposable {
	private readonly string _configFilePath;
	private readonly string _backupConfigFilePath;
	private readonly bool _hadExistingConfig;

	public ConfigStoreTests() {
		this._configFilePath = FilePathConstants.ConfigFilePath;
		this._backupConfigFilePath = this._configFilePath + ".bak";

		this._hadExistingConfig = File.Exists(this._configFilePath);
		if (this._hadExistingConfig) {
			File.Move(this._configFilePath, this._backupConfigFilePath);
		}
	}

	public void Dispose() {
		if (File.Exists(this._configFilePath)) {
			File.Delete(this._configFilePath);
		}

		if (this._hadExistingConfig) {
			File.Move(this._backupConfigFilePath, this._configFilePath);
		}
	}

	private IServiceProvider CreateServiceProvider() {
		var services = new ServiceCollection();
		services.AddTransient<ExtensionObjectModel>();
		services.AddTransient<ExecutionProgramObjectModel>();
		return services.BuildServiceProvider();
	}

	[Fact]
	public void Load_ThrowsException_CreatesDefaultConfig() {
		// Arrange
		Directory.CreateDirectory(Path.GetDirectoryName(this._configFilePath)!);
		// Write invalid JSON to force JsonSerializer.Deserialize to throw JsonException
		File.WriteAllText(this._configFilePath, "{ invalid json }");

		var serviceProvider = this.CreateServiceProvider();
		var services = new ServiceCollection();

		var mockConfig = new ConfigModel(new PathConfigModel(), new ScanConfigModel(serviceProvider), new ExecutionConfigModel(serviceProvider));
		services.AddSingleton(mockConfig);
		var storeServiceProvider = services.BuildServiceProvider();

		// Act
		var store = new ConfigStore(storeServiceProvider); // Load is called in constructor

		// Assert
		store.Config.ShouldNotBeNull();
		store.Config.ShouldBeSameAs(mockConfig); // Should fallback to DI
	}

	[Fact]
	public void Save_ThrowsException_DoesNotCrash() {
		// Arrange
		Directory.CreateDirectory(Path.GetDirectoryName(this._configFilePath)!);
		// Lock the file to force an IOException when Save tries to write to it
		using var fs = new FileStream(this._configFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);

		var serviceProvider = this.CreateServiceProvider();
		var services = new ServiceCollection();
		var mockConfig = new ConfigModel(new PathConfigModel(), new ScanConfigModel(serviceProvider), new ExecutionConfigModel(serviceProvider));
		services.AddSingleton(mockConfig);
		var storeServiceProvider = services.BuildServiceProvider();

		var store = new ConfigStore(storeServiceProvider);

		// Act & Assert
		// Should not throw
		Should.NotThrow(() => store.Save());
	}

	[Fact]
	public void Save_Success_WritesFile() {
		// Arrange
		var serviceProvider = this.CreateServiceProvider();
		var services = new ServiceCollection();
		var mockConfig = new ConfigModel(new PathConfigModel(), new ScanConfigModel(serviceProvider), new ExecutionConfigModel(serviceProvider));
		services.AddSingleton(mockConfig);
		var storeServiceProvider = services.BuildServiceProvider();

		var store = new ConfigStore(storeServiceProvider);

		// Ensure any existing file is deleted before the test
		if (File.Exists(this._configFilePath)) {
			File.Delete(this._configFilePath);
		}

		// Act
		store.Save();

		// Assert
		File.Exists(this._configFilePath).ShouldBeTrue();
		var fileContent = File.ReadAllText(this._configFilePath);
		fileContent.ShouldNotBeNullOrWhiteSpace();
	}
}