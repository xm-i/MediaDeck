using System.ComponentModel;
using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Stores.State.Model;
using MediaDeck.Composition.Stores.State.Model.Objects;
using MediaDeck.Core.Models.Files.Sort;
using Microsoft.Extensions.DependencyInjection;

namespace MediaDeck.Core.Tests.Models.Files.Sort;

/// <summary>
/// <see cref="SortSelector"/> のユニットテスト
/// </summary>
public class SortSelectorTests {
	private readonly IServiceProvider _serviceProvider;
	private readonly StateModel _stateModel;

	public SortSelectorTests() {
		var services = new ServiceCollection();
		services.AddTransient<SortObject>(sp => new SortObject(sp));
		services.AddTransient<SortItemObject>();
		services.AddSingleton<SearchStateModel>();
		services.AddSingleton<FolderManagerStateModel>();
		services.AddSingleton<ViewerStateModel>();
		services.AddSingleton<StateModel>();

		this._serviceProvider = services.BuildServiceProvider();
		this._stateModel = this._serviceProvider.GetRequiredService<StateModel>();
	}

	/// <summary>
	/// カレントソート条件が null の場合、元の配列をそのまま返すことを確認します。
	/// </summary>
	[Fact]
	public void SetSortConditions_ReturnsOriginalArray_WhenCurrentSortConditionIsNull() {
		// Arrange
		var selector = new SortSelector(this._stateModel);
		var files = new[] { new TestFileModel { Id = 1 }, new TestFileModel { Id = 2 } };

		// Act
		var result = selector.SetSortConditions(files);

		// Assert
		Assert.Same(files, result);
	}

	/// <summary>
	/// ソート条件オブジェクトが空の場合、<see cref="InvalidOperationException"/> が送出されることを確認します。
	/// </summary>
	[Fact]
	public void SetSortConditions_ThrowsInvalidOperationException_WhenSortItemObjectsIsEmpty() {
		// Arrange
		var sortObject = this._stateModel.SearchState.AddSortCondition();
		this._stateModel.SearchState.CurrentSortCondition.Value = sortObject.Id;

		var selector = new SortSelector(this._stateModel);
		var files = new[] { new TestFileModel { Id = 1 } };

		// Act & Assert
		Assert.Throws<InvalidOperationException>(() => selector.SetSortConditions(files));
	}

	/// <summary>
	/// 単一プロパティの昇順ソートが正しく適用されることを確認します。
	/// </summary>
	[Fact]
	public void SetSortConditions_SortsBySingleProperty_Ascending() {
		// Arrange
		var sortObject = this._stateModel.SearchState.AddSortCondition();
		var item = sortObject.AddSortItemObject();
		item.SortItemKey = SortItemKey.FilePath;
		item.Direction = ListSortDirection.Ascending;

		this._stateModel.SearchState.CurrentSortCondition.Value = sortObject.Id;
		this._stateModel.SearchState.SortDirection.Value = ListSortDirection.Ascending;

		var selector = new SortSelector(this._stateModel);
		var files = new[]
		{
			new TestFileModel { FilePath = "B" },
			new TestFileModel { FilePath = "C" },
			new TestFileModel { FilePath = "A" }
		};

		// Act
		var result = selector.SetSortConditions(files).Cast<TestFileModel>().ToList();

		// Assert
		Assert.Equal("A", result[0].FilePath);
		Assert.Equal("B", result[1].FilePath);
		Assert.Equal("C", result[2].FilePath);
	}

	/// <summary>
	/// 全体方向が降順に指定された場合、ソート結果が反転することを確認します。
	/// </summary>
	[Fact]
	public void SetSortConditions_SortsBySingleProperty_Descending() {
		// Arrange
		var sortObject = this._stateModel.SearchState.AddSortCondition();
		var item = sortObject.AddSortItemObject();
		item.SortItemKey = SortItemKey.FilePath;
		item.Direction = ListSortDirection.Ascending; // Item's base direction is Asc

		this._stateModel.SearchState.CurrentSortCondition.Value = sortObject.Id;
		this._stateModel.SearchState.SortDirection.Value = ListSortDirection.Descending; // Overall reverse is Desc

		var selector = new SortSelector(this._stateModel);
		var files = new[]
		{
			new TestFileModel { FilePath = "B" },
			new TestFileModel { FilePath = "C" },
			new TestFileModel { FilePath = "A" }
		};

		// Act
		var result = selector.SetSortConditions(files).Cast<TestFileModel>().ToList();

		// Assert
		Assert.Equal("C", result[0].FilePath);
		Assert.Equal("B", result[1].FilePath);
		Assert.Equal("A", result[2].FilePath);
	}

	/// <summary>
	/// 複数プロパティによるソート（ThenBy ロジック）が正しく適用されることを確認します。
	/// </summary>
	[Fact]
	public void SetSortConditions_SortsByMultipleProperties() {
		// Arrange
		var sortObject = this._stateModel.SearchState.AddSortCondition();

		var item1 = sortObject.AddSortItemObject();
		item1.SortItemKey = SortItemKey.Rate;
		item1.Direction = ListSortDirection.Descending; // Higher rate first

		var item2 = sortObject.AddSortItemObject();
		item2.SortItemKey = SortItemKey.FilePath;
		item2.Direction = ListSortDirection.Ascending;

		this._stateModel.SearchState.CurrentSortCondition.Value = sortObject.Id;
		this._stateModel.SearchState.SortDirection.Value = ListSortDirection.Ascending;

		var selector = new SortSelector(this._stateModel);
		var files = new[]
		{
			new TestFileModel { Rate = 5, FilePath = "B" },
			new TestFileModel { Rate = 3, FilePath = "A" },
			new TestFileModel { Rate = 5, FilePath = "A" }
		};

		// Act
		var result = selector.SetSortConditions(files).Cast<TestFileModel>().ToList();

		// Assert
		// Rate=5 が最優先。その中で FilePath が A -> B の順
		Assert.Equal(5, result[0].Rate);
		Assert.Equal("A", result[0].FilePath);

		Assert.Equal(5, result[1].Rate);
		Assert.Equal("B", result[1].FilePath);

		Assert.Equal(3, result[2].Rate);
		Assert.Equal("A", result[2].FilePath);
	}
}