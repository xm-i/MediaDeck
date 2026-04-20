using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.Services;
using MediaDeck.Composition.Stores.State.Model;
using MediaDeck.Composition.Stores.State.Model.Objects;
using MediaDeck.Core.Models.Files.Sort;
using MediaDeck.Core.Models.NotificationDispatcher;
using MediaDeck.Core.Stores.State;
using MediaDeck.Store.State;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace MediaDeck.Core.Tests.Models.Files.Sort;

/// <summary>
/// <see cref="SortManager"/> のユニットテスト
/// </summary>
public class SortManagerTests {
	private readonly IStateStore _stateStore;
	private readonly Mock<IServiceProvider> _mockServiceProvider;
	private readonly SearchDefinitionsStateModel _searchDefinitions;
	private readonly TabStateModel _tabState;

	public SortManagerTests() {
		var services = new ServiceCollection();
		services.AddTransient<SortObject>(sp => new SortObject(sp));
		services.AddTransient<SortItemObject>();
		services.AddSingleton<SearchStateModel>();
		services.AddSingleton<FolderManagerStateModel>();
		services.AddSingleton<ViewerStateModel>();
		services.AddSingleton<AppStateModel>();
		services.AddSingleton<TabStateModel>();
		services.AddSingleton<SearchDefinitionsStateModel>();
		services.AddLogging();
		services.AddSingleton<AppNotificationDispatcher>();

		var realServiceProvider = services.BuildServiceProvider();
		this._searchDefinitions = realServiceProvider.GetRequiredService<SearchDefinitionsStateModel>();
		this._tabState = realServiceProvider.GetRequiredService<TabStateModel>();

		this._mockServiceProvider = new Mock<IServiceProvider>();
		var mockScope = new Mock<IServiceScope>();
		var mockScopeFactory = new Mock<IServiceScopeFactory>();

		this._mockServiceProvider.Setup(x => x.GetService(typeof(IServiceScopeFactory))).Returns(mockScopeFactory.Object);
		mockScopeFactory.Setup(x => x.CreateScope()).Returns(mockScope.Object);
		mockScope.Setup(x => x.ServiceProvider).Returns(realServiceProvider);

		this._mockServiceProvider.Setup(x => x.GetService(typeof(ILogger<StateStore>))).Returns(realServiceProvider.GetRequiredService<ILogger<StateStore>>());
		this._mockServiceProvider.Setup(x => x.GetService(typeof(AppNotificationDispatcher))).Returns(realServiceProvider.GetRequiredService<AppNotificationDispatcher>());

		// Required for StateStore correctly resolving RootStateModel when loaded via DI if Load uses sp
		this._mockServiceProvider.Setup(x => x.GetService(typeof(AppStateModel))).Returns(realServiceProvider.GetRequiredService<AppStateModel>());

		var pathProvider = new StubAppPathProvider();
		this._mockServiceProvider.Setup(x => x.GetService(typeof(IAppPathProvider))).Returns(pathProvider);

		this._stateStore = new StateStore(this._mockServiceProvider.Object, pathProvider);
	}

	/// <summary>
	/// ソートロジックが未ソートのリストを正しい順序でソートすることを確認します。
	/// </summary>
	[Fact]
	public void SortLogic_OrdersUnsortedListCorrectly() {
		// Arrange
		var sortManager = new SortManager(this._stateStore, this._searchDefinitions);
		sortManager.AddCondition();
		var sortObject = sortManager.SortConditions.Last();

		var item = sortObject.AddSortItemObject();
		item.SortItemKey = SortItemKey.FilePath;
		item.Direction = System.ComponentModel.ListSortDirection.Ascending;

		this._tabState.SearchState.CurrentSortCondition.Value = sortObject.Id;
		this._tabState.SearchState.SortDirection.Value = System.ComponentModel.ListSortDirection.Ascending;

		var selector = new SortSelector(this._tabState, this._searchDefinitions);
		var unsortedList = new[]
		{
			new TestFileModel { FilePath = "FileC.txt" },
			new TestFileModel { FilePath = "FileA.txt" },
			new TestFileModel { FilePath = "FileB.txt" }
		};

		// Act
		var result = selector.SetSortConditions(unsortedList.AsQueryable()).Cast<TestFileModel>().ToList();

		// Assert
		Assert.Equal("FileA.txt", result[0].FilePath);
		Assert.Equal("FileB.txt", result[1].FilePath);
		Assert.Equal("FileC.txt", result[2].FilePath);
	}

	/// <summary>
	/// コンストラクタが <see cref="StateStore"/> からソート条件を正しく取得することを確認します。
	/// </summary>
	[Fact]
	public void Constructor_SetsSortConditionsFromStateStore() {
		// Act
		var sortManager = new SortManager(this._stateStore, this._searchDefinitions);

		// Assert
		Assert.Same(this._searchDefinitions.SortConditions, sortManager.SortConditions);
	}

	/// <summary>
	/// <see cref="SortManager.AddCondition"/> が検索状態に新しいソート条件を追加することを確認します。
	/// </summary>
	[Fact]
	public void AddCondition_AddsNewSortCondition() {
		// Arrange
		var sortManager = new SortManager(this._stateStore, this._searchDefinitions);
		var initialCount = sortManager.SortConditions.Count;

		// Act
		sortManager.AddCondition();

		// Assert
		Assert.Equal(initialCount + 1, sortManager.SortConditions.Count);
	}

	/// <summary>
	/// <see cref="SortManager.RemoveCondition"/> が検索状態から指定したソート条件を削除することを確認します。
	/// </summary>
	[Fact]
	public void RemoveCondition_RemovesTargetSortCondition() {
		// Arrange
		var sortManager = new SortManager(this._stateStore, this._searchDefinitions);
		sortManager.AddCondition();
		var conditionToRemove = sortManager.SortConditions.Last();
		var initialCount = sortManager.SortConditions.Count;

		// Act
		sortManager.RemoveCondition(conditionToRemove);

		// Assert
		Assert.Equal(initialCount - 1, sortManager.SortConditions.Count);
		Assert.DoesNotContain(conditionToRemove, sortManager.SortConditions);
	}

	/// <summary>
	/// <see cref="SortManager.Save"/> メソッドの実行中に例外が発生しないことを確認します。
	/// </summary>
	[Fact]
	public void Save_ExecutesWithoutException() {
		// Arrange
		var sortManager = new SortManager(this._stateStore, this._searchDefinitions);

		// Act
		var exception = Record.Exception(() => sortManager.Save());

		// Assert
		Assert.Null(exception);
	}
}