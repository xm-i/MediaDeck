using System.Runtime.CompilerServices;
using MediaDeck.Composition.Stores.Config.Model;
using MediaDeck.Store.Config;

using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace MediaDeck.Core.Tests.Stores.Config;

public class ConfigStoreTests : IDisposable {
	private readonly string _tempDirectory;
	private readonly string _testConfigFilePath;

	// We create a testable wrapper to override the virtual ConfigFilePath property
	private class TestableConfigStore : ConfigStore {
		// Use a static property to bypass the "virtual member call in constructor" issue
		// so that the path is available when the base constructor calls Load().
		public static string TestPath { get; set; } = string.Empty;

		protected override string ConfigFilePath {
			get {
				return TestPath;
			}
		}

		public TestableConfigStore(IServiceProvider service) : base(service) {
		}
	}

	public ConfigStoreTests() {
		this._tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
		Directory.CreateDirectory(this._tempDirectory);
		this._testConfigFilePath = Path.Combine(this._tempDirectory, "MediaDeck.config");

		TestableConfigStore.TestPath = this._testConfigFilePath;
	}

	public void Dispose() {
		if (Directory.Exists(this._tempDirectory)) {
			Directory.Delete(this._tempDirectory, true);
		}
	}


	[Fact]
	public void Load_ThrowsException_CreatesDefaultConfig() {
		// Arrange
		Directory.CreateDirectory(Path.GetDirectoryName(this._testConfigFilePath)!);
		// Write invalid JSON to force JsonSerializer.Deserialize to throw JsonException
		File.WriteAllText(this._testConfigFilePath, "{ invalid json }");

		var services = new ServiceCollection();
		var mockConfig = (ConfigModel)RuntimeHelpers.GetUninitializedObject(typeof(ConfigModel));
		services.AddSingleton(mockConfig);
		var serviceProvider = services.BuildServiceProvider();

		// Act
		var store = new TestableConfigStore(serviceProvider); // Load is called in constructor

		// Assert
		store.Config.ShouldNotBeNull();
		store.Config.ShouldBeSameAs(mockConfig); // Should fallback to DI
	}

	[Fact]
	public void Save_ThrowsException_DoesNotCrash() {
		// Arrange
		Directory.CreateDirectory(Path.GetDirectoryName(this._testConfigFilePath)!);
		// Lock the file to force an IOException when Save tries to write to it
		using var fs = new FileStream(this._testConfigFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);

		var services = new ServiceCollection();
		var mockConfig = (ConfigModel)RuntimeHelpers.GetUninitializedObject(typeof(ConfigModel));
		services.AddSingleton(mockConfig);
		var serviceProvider = services.BuildServiceProvider();

		var store = new TestableConfigStore(serviceProvider);

		// Act & Assert
		// Should not throw
		Should.NotThrow(() => store.Save());
	}

	[Fact]
	public void Save_WithInvalidFilePath_DoesNotThrow() {
		// Arrange
		TestableConfigStore.TestPath = string.Empty;

		var services = new ServiceCollection();
		var mockConfig = (ConfigModel)RuntimeHelpers.GetUninitializedObject(typeof(ConfigModel));
		services.AddSingleton(mockConfig);
		var serviceProvider = services.BuildServiceProvider();

		var store = new TestableConfigStore(serviceProvider);

		// Act & Assert
		// Calling Save with an empty file path will cause an exception in Path.GetDirectoryName or File.WriteAllText
		// The test ensures the exception is caught and does not crash the application
		Should.NotThrow(() => store.Save());
	}

	[Fact]
	public void Save_WithInvalidDirectoryCondition_DoesNotThrow() {
		// Arrange: Set up an invalid directory condition that works cross-platform.
		// By setting the target file path to inside a file that already exists, Directory.CreateDirectory will throw an IOException.
		var tempFile = Path.GetTempFileName();
		TestableConfigStore.TestPath = Path.Combine(tempFile, "invalid_dir", "test.config");

		try {
			var services = new ServiceCollection();
			var mockConfig = (ConfigModel)RuntimeHelpers.GetUninitializedObject(typeof(ConfigModel));
			services.AddSingleton(mockConfig);
			var serviceProvider = services.BuildServiceProvider();

			var store = new TestableConfigStore(serviceProvider);

			// Act & Assert
			Should.NotThrow(() => store.Save());
		} finally {
			if (File.Exists(tempFile)) {
				File.Delete(tempFile);
			}
		}
	}
}