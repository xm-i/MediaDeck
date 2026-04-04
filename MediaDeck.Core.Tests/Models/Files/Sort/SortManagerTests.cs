using MediaDeck.Composition.Stores.State.Model;
using MediaDeck.Composition.Stores.State.Model.Objects;
using MediaDeck.Core.Models.Files.Sort;
using MediaDeck.Core.Stores.State;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using ObservableCollections;

namespace MediaDeck.Core.Tests.Models.Files.Sort;

/// <summary>
/// <see cref="SortManager"/> のユニットテスト
/// </summary>
public class SortManagerTests
{
    private readonly StateStore _stateStore;
    private readonly Mock<IServiceProvider> _mockServiceProvider;

    public SortManagerTests()
    {
        var services = new ServiceCollection();
        services.AddTransient<SortObject>(sp => new SortObject(sp));
        services.AddTransient<SortItemObject>();
        services.AddSingleton<SearchStateModel>();
        services.AddSingleton<FolderManagerStateModel>();
        services.AddSingleton<ViewerStateModel>();
        services.AddSingleton<StateModel>();

        var realServiceProvider = services.BuildServiceProvider();

        this._mockServiceProvider = new Mock<IServiceProvider>();
        var mockScope = new Mock<IServiceScope>();
        var mockScopeFactory = new Mock<IServiceScopeFactory>();

        this._mockServiceProvider.Setup(x => x.GetService(typeof(IServiceScopeFactory))).Returns(mockScopeFactory.Object);
        mockScopeFactory.Setup(x => x.CreateScope()).Returns(mockScope.Object);
        mockScope.Setup(x => x.ServiceProvider).Returns(realServiceProvider);

        this._stateStore = new StateStore(this._mockServiceProvider.Object);
    }

    /// <summary>
    /// コンストラクタが <see cref="StateStore"/> からソート条件を正しく取得することを確認します。
    /// </summary>
    [Fact]
    public void Constructor_SetsSortConditionsFromStateStore()
    {
        // Act
        var sortManager = new SortManager(this._stateStore);

        // Assert
        Assert.Same(this._stateStore.State.SearchState.SortConditions, sortManager.SortConditions);
    }

    /// <summary>
    /// <see cref="SortManager.AddCondition"/> が検索状態に新しいソート条件を追加することを確認します。
    /// </summary>
    [Fact]
    public void AddCondition_AddsNewSortCondition()
    {
        // Arrange
        var sortManager = new SortManager(this._stateStore);
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
    public void RemoveCondition_RemovesTargetSortCondition()
    {
        // Arrange
        var sortManager = new SortManager(this._stateStore);
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
    public void Save_ExecutesWithoutException()
    {
        // Arrange
        var sortManager = new SortManager(this._stateStore);

        // Act
        var exception = Record.Exception(() => sortManager.Save());

        // Assert
        Assert.Null(exception);
    }
}
