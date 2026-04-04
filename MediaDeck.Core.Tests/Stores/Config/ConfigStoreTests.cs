using System;
using System.IO;
using System.Runtime.CompilerServices;
using MediaDeck.Composition.Constants;
using MediaDeck.Composition.Stores.Config.Model;
using MediaDeck.Core.Stores.Config;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Shouldly;
using Xunit;

namespace MediaDeck.Core.Tests.Stores.Config;

public class ConfigStoreTests : IDisposable
{
    private readonly string _configFilePath;
    private readonly string _backupConfigFilePath;
    private readonly bool _hadExistingConfig;

    public ConfigStoreTests()
    {
        _configFilePath = FilePathConstants.ConfigFilePath;
        _backupConfigFilePath = _configFilePath + ".bak";

        _hadExistingConfig = File.Exists(_configFilePath);
        if (_hadExistingConfig)
        {
            File.Move(_configFilePath, _backupConfigFilePath);
        }
    }

    public void Dispose()
    {
        if (File.Exists(_configFilePath))
        {
            File.Delete(_configFilePath);
        }

        if (_hadExistingConfig)
        {
            File.Move(_backupConfigFilePath, _configFilePath);
        }
    }

    [Fact]
    public void Load_ThrowsException_CreatesDefaultConfig()
    {
        // Arrange
        Directory.CreateDirectory(Path.GetDirectoryName(_configFilePath)!);
        // Write invalid JSON to force JsonSerializer.Deserialize to throw JsonException
        File.WriteAllText(_configFilePath, "{ invalid json }");

        var services = new ServiceCollection();
        var mockConfig = (ConfigModel)RuntimeHelpers.GetUninitializedObject(typeof(ConfigModel));
        services.AddSingleton(mockConfig);
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var store = new ConfigStore(serviceProvider); // Load is called in constructor

        // Assert
        store.Config.ShouldNotBeNull();
        store.Config.ShouldBeSameAs(mockConfig); // Should fallback to DI
    }

    [Fact]
    public void Save_ThrowsException_DoesNotCrash()
    {
        // Arrange
        Directory.CreateDirectory(Path.GetDirectoryName(_configFilePath)!);
        // Lock the file to force an IOException when Save tries to write to it
        using var fs = new FileStream(_configFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);

        var services = new ServiceCollection();
        var mockConfig = (ConfigModel)RuntimeHelpers.GetUninitializedObject(typeof(ConfigModel));
        services.AddSingleton(mockConfig);
        var serviceProvider = services.BuildServiceProvider();

        var store = new ConfigStore(serviceProvider);

        // Act & Assert
        // Should not throw
        Should.NotThrow(() => store.Save());
    }
}
