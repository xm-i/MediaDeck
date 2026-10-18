using System.Runtime.CompilerServices;
using System.Text.Json.Serialization.Metadata;
using MediaDeck.Composition.Interfaces.Services;
using MediaDeck.Composition.Stores.State.Model;
using MediaDeck.Core.Models.NotificationDispatcher;
using MediaDeck.Store.State;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;

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

		public TestableStateStore(IServiceProvider service, IAppPathProvider pathProvider) : base(service, pathProvider) {
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

		this._serviceProviderMock.Setup(x => x.GetService(typeof(AppStateModel)))
			.Returns((AppStateModel)RuntimeHelpers.GetUninitializedObject(typeof(AppStateModel)));

		this._serviceProviderMock.Setup(x => x.GetService(typeof(ILogger<StateStore>)))
			.Returns(NullLogger<StateStore>.Instance);

		this._serviceProviderMock.Setup(x => x.GetService(typeof(AppNotificationDispatcher)))
			.Returns(new AppNotificationDispatcher());

		this._tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
		Directory.CreateDirectory(this._tempDirectory);
		this._testStateFilePath = Path.Combine(this._tempDirectory, "MediaDeck.states");

		var mockPathProvider = new Mock<IAppPathProvider>();
		mockPathProvider.Setup(x => x.StateFilePath).Returns(() => TestableStateStore.TestPath);

		this._serviceProviderMock.Setup(x => x.GetService(typeof(IAppPathProvider))).Returns(mockPathProvider.Object);

		TestableStateStore.TestPath = this._testStateFilePath;
	}

	public void Dispose() {
		if (Directory.Exists(this._tempDirectory)) {
			Directory.Delete(this._tempDirectory, true);
		}
	}

	[Fact]
	public void Load_WhenExceptionOccurs_UsesFallbackAppStateModel() {
		File.WriteAllText(this._testStateFilePath, "{ invalid json }");

		// The constructor invokes Load automatically
		var store = new TestableStateStore(this._serviceProviderMock.Object, this._serviceProviderMock.Object.GetRequiredService<IAppPathProvider>());

		store.RootState.AppState.ShouldNotBeNull();
		this._serviceProviderMock.Verify(x => x.GetService(typeof(AppStateModel)), Times.Once);
	}

	[Fact]
	public void Save_WhenExceptionOccurs_DoesNotThrow() {
		// To cause an exception, we will lock the file exclusively
		using var stream = new FileStream(this._testStateFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);

		// This invokes Load, which sees an empty/locked file or fails to read, hitting the fallback
		var store = new TestableStateStore(this._serviceProviderMock.Object, this._serviceProviderMock.Object.GetRequiredService<IAppPathProvider>());

		// Save tries to write, hitting the lock
		var act = () => store.Save();
		Should.NotThrow(act);
	}

	[Fact]
	public void Load_WhenFileNotExists_UsesFallbackAppStateModel() {
		if (File.Exists(this._testStateFilePath)) {
			File.Delete(this._testStateFilePath);
		}

		var store = new TestableStateStore(this._serviceProviderMock.Object, this._serviceProviderMock.Object.GetRequiredService<IAppPathProvider>());

		store.RootState.AppState.ShouldNotBeNull();
		this._serviceProviderMock.Verify(x => x.GetService(typeof(AppStateModel)), Times.Once);
	}

	[Fact]
	public void Save_WithInvalidFilePath_DoesNotThrow() {
		TestableStateStore.TestPath = string.Empty;

		var store = new TestableStateStore(this._serviceProviderMock.Object, this._serviceProviderMock.Object.GetRequiredService<IAppPathProvider>());

		var act = () => store.Save();
		Should.NotThrow(act);
	}

	/// <summary>
	/// 複数タブを含む <see cref="RootStateModel"/> のシリアライズ・デシリアライズで各タブの <see cref="TabStateModel"/> が上書きされず個別に復元され、かつ各タブスコープ内では同一インスタンスとして解決されることを確認します。
	/// </summary>
	[Fact]
	public void RootState_SerializeAndDeserialize_RestoresMultipleTabStates() {
		var services = new ServiceCollection();
		MediaDeck.Composition.DIRegistration.AddGeneratedServices(services);
		using var sp = services.BuildServiceProvider();

		var root = sp.GetRequiredService<RootStateModel>();

		using var scope1 = sp.CreateScope();
		var tabState1 = scope1.ServiceProvider.GetRequiredService<TabStateModel>();
		scope1.ServiceProvider.GetRequiredService<TabStateModel>().ShouldBeSameAs(tabState1);
		tabState1.DisplayName.Value = "Test Tab 1";
		tabState1.LeftPaneWidth.Value = 320;
		root.Tabs.Add(tabState1);

		using var scope2 = sp.CreateScope();
		var tabState2 = scope2.ServiceProvider.GetRequiredService<TabStateModel>();
		tabState2.DisplayName.Value = "Test Tab 2";
		tabState2.LeftPaneWidth.Value = 400;
		root.Tabs.Add(tabState2);

		tabState1.ShouldNotBeSameAs(tabState2);
		tabState1.TabId.ShouldNotBe(tabState2.TabId);

		var jsonDto = RootStateModelForJson.CreateJson(root);
		var options = new System.Text.Json.JsonSerializerOptions {
			TypeInfoResolver = MediaDeck.Store.SerializerContext.StateJsonSerializerContext.Default
				.WithAddedModifier(global::GenJsonConfig.ForJsonConverterRegistry.ApplyPolymorphism)
		};
		var json = System.Text.Json.JsonSerializer.Serialize(jsonDto, options);

		using var loadScope = sp.CreateScope();
		var loadedDto = System.Text.Json.JsonSerializer.Deserialize<RootStateModelForJson>(json, options);
		var restored = RootStateModelForJson.CreateModel(loadedDto!, loadScope.ServiceProvider);

		restored.Tabs.Count.ShouldBe(2);
		restored.Tabs[0].ShouldNotBeSameAs(restored.Tabs[1]);
		restored.Tabs[0].TabId.ShouldBe(tabState1.TabId);
		restored.Tabs[0].DisplayName.Value.ShouldBe("Test Tab 1");
		restored.Tabs[0].LeftPaneWidth.Value.ShouldBe(320);
		restored.Tabs[1].TabId.ShouldBe(tabState2.TabId);
		restored.Tabs[1].DisplayName.Value.ShouldBe("Test Tab 2");
		restored.Tabs[1].LeftPaneWidth.Value.ShouldBe(400);

		// デシリアライズ時に生成された各タブ固有スコープ内で同一の TabStateModel インスタンスが解決されることを確認
		restored.Tabs[0].ServiceProvider.ShouldNotBeSameAs(restored.Tabs[1].ServiceProvider);
		restored.Tabs[0].ServiceProvider.GetRequiredService<TabStateModel>().ShouldBeSameAs(restored.Tabs[0]);
		restored.Tabs[1].ServiceProvider.GetRequiredService<TabStateModel>().ShouldBeSameAs(restored.Tabs[1]);

		foreach (var tab in restored.Tabs) {
			(tab.ServiceProvider as IDisposable)?.Dispose();
		}
	}
}