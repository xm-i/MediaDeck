using System;
using System.IO;
using System.Runtime.CompilerServices;
using MediaDeck.Composition.Constants;
using MediaDeck.Composition.Stores.State.Model;
using MediaDeck.Core.Stores.State;
using MediaDeck.Store.State;

using Microsoft.Extensions.DependencyInjection;
using Moq;
using Shouldly;
using Xunit;

namespace MediaDeck.Core.Tests.Stores.State;

public class StateStoreTests : IDisposable {
	private readonly Mock<IServiceProvider> _serviceProviderMock;
	private readonly Mock<IServiceScopeFactory> _serviceScopeFactoryMock;
	private readonly Mock<IServiceScope> _serviceScopeMock;

	private readonly string _tempDirectory;
	private readonly string _testStateFilePath;

	// We create a testable wrapper to override the virtual StateFilePath property
	private class TestableStateStore : StateStore {
		// Use a static property to bypass the "virtual member call in constructor" issue
		// so that the path is available when the base constructor calls Load().
		public static string TestPath { get; set; } = string.Empty;

		protected override string StateFilePath {
			get {
				return TestPath;
			}
		}

		public TestableStateStore(IServiceProvider service) : base(service) {
		}
	}

	public StateStoreTests() {
		this._serviceProviderMock = new Mock<IServiceProvider>();
		this._serviceScopeFactoryMock = new Mock<IServiceScopeFactory>();
		this._serviceScopeMock = new Mock<IServiceScope>();

		this._serviceProviderMock.Setup(x => x.GetService(typeof(IServiceScopeFactory)))
			.Returns(this._serviceScopeFactoryMock.Object);

		this._serviceScopeFactoryMock.Setup(x => x.CreateScope())
			.Returns(this._serviceScopeMock.Object);

		this._serviceScopeMock.Setup(x => x.ServiceProvider)
			.Returns(this._serviceProviderMock.Object);

		this._serviceProviderMock.Setup(x => x.GetService(typeof(StateModel)))
			.Returns((StateModel)RuntimeHelpers.GetUninitializedObject(typeof(StateModel)));

		this._tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
		Directory.CreateDirectory(this._tempDirectory);
		this._testStateFilePath = Path.Combine(this._tempDirectory, "MediaDeck.states");

		TestableStateStore.TestPath = this._testStateFilePath;
	}

	public void Dispose() {
		if (Directory.Exists(this._tempDirectory)) {
			Directory.Delete(this._tempDirectory, true);
		}
	}

	[Fact]
	public void Load_WhenExceptionOccurs_UsesFallbackStateModel() {
		File.WriteAllText(this._testStateFilePath, "{ invalid json }");

		// The constructor invokes Load automatically
		var store = new TestableStateStore(this._serviceProviderMock.Object);

		store.State.ShouldNotBeNull();
		this._serviceProviderMock.Verify(x => x.GetService(typeof(StateModel)), Times.Once);
	}

	[Fact]
	public void Save_WhenExceptionOccurs_DoesNotThrow() {
		// To cause an exception, we will lock the file exclusively
		using var stream = new FileStream(this._testStateFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);

		// This invokes Load, which sees an empty/locked file or fails to read, hitting the fallback
		var store = new TestableStateStore(this._serviceProviderMock.Object);

		// Save tries to write, hitting the lock
		var act = () => store.Save();
		Should.NotThrow(act);
	}

	[Fact]
	public void Load_WhenFileNotExists_UsesFallbackStateModel() {
		if (File.Exists(this._testStateFilePath)) {
			File.Delete(this._testStateFilePath);
		}

		var store = new TestableStateStore(this._serviceProviderMock.Object);

		store.State.ShouldNotBeNull();
		this._serviceProviderMock.Verify(x => x.GetService(typeof(StateModel)), Times.Once);
	}

	[Fact]
	public void Save_WithInvalidFilePath_DoesNotThrow() {
		TestableStateStore.TestPath = string.Empty;

		var store = new TestableStateStore(this._serviceProviderMock.Object);

		var act = () => store.Save();
		Should.NotThrow(act);
	}
}