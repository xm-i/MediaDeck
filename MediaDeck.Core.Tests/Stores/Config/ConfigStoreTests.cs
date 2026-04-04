using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using FluentAssertions;
using MediaDeck.Composition.Constants;
using MediaDeck.Core.Stores.Config;
using Moq;
using Xunit;

namespace MediaDeck.Core.Tests.Stores.Config;

public class ConfigStoreTests
{
    [Fact]
    public void Save_ShouldHandleException_WhenFilePathIsInvalid()
    {
        // Arrange
        // Create an uninitialized object to bypass the constructor which calls Load() and requires dependency injection setup
        var configStore = (ConfigStore)RuntimeHelpers.GetUninitializedObject(typeof(ConfigStore));

        // Let's modify the exception simulation approach.
        // If we can't easily overwrite the static initonly backing field without unsafe code in newer .NET,
        // we can set up an invalid state another way if possible.
        // Wait, setting initonly fields via reflection used to work but is blocked in .NET 10.
        // Another approach: since `ConfigFilePath` evaluates to `Environment.SpecialFolder.ApplicationData + "\\MediaDeck\\MediaDeck.config"`,
        // and we cannot easily change `Environment.SpecialFolder.ApplicationData`, what if we pre-create a directory at that path?
        // Wait, the path is %APPDATA%\MediaDeck\MediaDeck.config. We can create a folder named `MediaDeck.config`.
        // `File.WriteAllText` will throw `UnauthorizedAccessException` when it tries to write to a path that is actually a directory.

        var configFilePath = FilePathConstants.ConfigFilePath;
        var isCreatedByTest = false;
        var directoryExisted = Directory.Exists(configFilePath);
        var fileExisted = File.Exists(configFilePath);

        string? tempFileBackup = null;

        try
        {
            if (fileExisted)
            {
                // Temporarily backup the file if it exists
                tempFileBackup = Path.GetTempFileName();
                File.Copy(configFilePath, tempFileBackup, true);
                File.Delete(configFilePath);
            }

            if (!directoryExisted && !fileExisted)
            {
                Directory.CreateDirectory(configFilePath);
                isCreatedByTest = true;
            }

            // Act
            Action act = () => configStore.Save();

            // Assert
            act.Should().NotThrow();
        }
        finally
        {
            // Clean up
            if (isCreatedByTest && Directory.Exists(configFilePath))
            {
                Directory.Delete(configFilePath);
            }

            if (tempFileBackup != null && File.Exists(tempFileBackup))
            {
                File.Copy(tempFileBackup, configFilePath, true);
                File.Delete(tempFileBackup);
            }
        }
    }
}
