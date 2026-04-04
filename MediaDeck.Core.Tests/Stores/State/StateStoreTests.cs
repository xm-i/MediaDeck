using System;
using System.IO;
using System.Runtime.CompilerServices;
using FluentAssertions;
using MediaDeck.Composition.Constants;
using MediaDeck.Composition.Stores.State.Model;
using MediaDeck.Core.Stores.State;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace MediaDeck.Core.Tests.Stores.State;

public class StateStoreTests : IDisposable
{
    private readonly Mock<IServiceProvider> _serviceProviderMock;
    private readonly Mock<IServiceScopeFactory> _serviceScopeFactoryMock;
    private readonly Mock<IServiceScope> _serviceScopeMock;

    private readonly string _tempDirectory;
    private readonly string _testStateFilePath;

    // We create a testable wrapper to override the virtual StateFilePath property
    private class TestableStateStore : StateStore
    {
        // Use a static property to bypass the "virtual member call in constructor" issue
        // so that the path is available when the base constructor calls Load().
        public static string TestPath { get; set; } = string.Empty;

        protected override string StateFilePath => TestPath;

        public TestableStateStore(IServiceProvider service) : base(service)
        {
        }
    }

    public StateStoreTests()
    {
        _serviceProviderMock = new Mock<IServiceProvider>();
        _serviceScopeFactoryMock = new Mock<IServiceScopeFactory>();
        _serviceScopeMock = new Mock<IServiceScope>();

        _serviceProviderMock.Setup(x => x.GetService(typeof(IServiceScopeFactory)))
            .Returns(_serviceScopeFactoryMock.Object);

        _serviceScopeFactoryMock.Setup(x => x.CreateScope())
            .Returns(_serviceScopeMock.Object);

        _serviceScopeMock.Setup(x => x.ServiceProvider)
            .Returns(_serviceProviderMock.Object);

        _serviceProviderMock.Setup(x => x.GetService(typeof(StateModel)))
            .Returns((StateModel)RuntimeHelpers.GetUninitializedObject(typeof(StateModel)));

        _tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDirectory);
        _testStateFilePath = Path.Combine(_tempDirectory, "MediaDeck.states");

        TestableStateStore.TestPath = _testStateFilePath;
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, true);
        }
    }

    [Fact]
    public void Load_WhenExceptionOccurs_UsesFallbackStateModel()
    {
        File.WriteAllText(_testStateFilePath, "{ invalid json }");

        // The constructor invokes Load automatically
        var store = new TestableStateStore(_serviceProviderMock.Object);

        store.State.Should().NotBeNull();
        _serviceProviderMock.Verify(x => x.GetService(typeof(StateModel)), Times.Once);
    }

    [Fact]
    public void Save_WhenExceptionOccurs_DoesNotThrow()
    {
        // To cause an exception, we will lock the file exclusively
        using var stream = new FileStream(_testStateFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);

        // This invokes Load, which sees an empty/locked file or fails to read, hitting the fallback
        var store = new TestableStateStore(_serviceProviderMock.Object);

        // Save tries to write, hitting the lock
        var act = () => store.Save();
        act.Should().NotThrow();
    }

    [Fact]
    public void Load_WhenFileNotExists_UsesFallbackStateModel()
    {
        if (File.Exists(_testStateFilePath))
        {
            File.Delete(_testStateFilePath);
        }

        var store = new TestableStateStore(_serviceProviderMock.Object);

        store.State.Should().NotBeNull();
        _serviceProviderMock.Verify(x => x.GetService(typeof(StateModel)), Times.Once);
    }
}
