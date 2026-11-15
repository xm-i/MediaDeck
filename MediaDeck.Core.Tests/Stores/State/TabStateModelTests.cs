using System.ComponentModel;
using System.Text.Json;
using MediaDeck.Composition.Constants;
using MediaDeck.Composition.Stores.State.Model;
using MediaDeck.Store.SerializerContext;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Shouldly;

namespace MediaDeck.Core.Tests.Stores.State;

public class TabStateModelTests {
	[Fact]
	public void ViewerStateModel_HasDefaultColumnsOnInitialization() {
		var viewerState = new ViewerStateModel();

		viewerState.ListColumns.Count.ShouldBe(5);
		viewerState.ListColumns.Any(x => x.ColumnId == ListViewerColumnIds.FileName).ShouldBeTrue();
		viewerState.ListColumns.Any(x => x.ColumnId == ListViewerColumnIds.Resolution).ShouldBeTrue();
		viewerState.ListColumns.Any(x => x.ColumnId == ListViewerColumnIds.FileSize).ShouldBeTrue();
		viewerState.ListColumns.Any(x => x.ColumnId == ListViewerColumnIds.CreationTime).ShouldBeTrue();
		viewerState.ListColumns.Any(x => x.ColumnId == ListViewerColumnIds.Rate).ShouldBeTrue();
	}

	[Fact]
	public void ViewerStateModel_GetOrCreateColumnState_ReturnsExistingOrCreatesNew() {
		var viewerState = new ViewerStateModel();
		var fileNameCol = viewerState.GetOrCreateColumnState(ListViewerColumnIds.FileName);
		fileNameCol.ShouldNotBeNull();
		fileNameCol.Width.Value.ShouldBe(400);

		var customCol = viewerState.GetOrCreateColumnState("CustomCol", 200, false);
		customCol.ShouldNotBeNull();
		customCol.Width.Value.ShouldBe(200);
		customCol.IsVisible.Value.ShouldBeFalse();
		viewerState.ListColumns.Count.ShouldBe(6);
	}

	[Fact]
	public void ViewerStateModel_CopyFrom_CopiesAllSettingsAndColumns() {
		var source = new ViewerStateModel();
		source.ItemSize.Value = 300;
		source.ActiveViewer.Value = ViewerType.List;
		source.ShowOverlay.Value = false;
		source.ShowInfo.Value = false;
		var fn = source.GetOrCreateColumnState(ListViewerColumnIds.FileName);
		fn.Width.Value = 550;
		fn.IsVisible.Value = false;

		var target = new ViewerStateModel();
		target.CopyFrom(source);

		target.ItemSize.Value.ShouldBe(300);
		target.ActiveViewer.Value.ShouldBe(ViewerType.List);
		target.ShowOverlay.Value.ShouldBeFalse();
		target.ShowInfo.Value.ShouldBeFalse();

		var targetFn = target.GetOrCreateColumnState(ListViewerColumnIds.FileName);
		targetFn.Width.Value.ShouldBe(550);
		targetFn.IsVisible.Value.ShouldBeFalse();
	}

	[Fact]
	public void SearchStateModel_CopyFrom_CopiesAllSettings() {
		var sortId = Guid.NewGuid();
		var filterId = Guid.NewGuid();

		var source = new SearchStateModel();
		source.CurrentSortCondition.Value = sortId;
		source.SortDirection.Value = ListSortDirection.Descending;
		source.CurrentFilteringConditions.Value = [filterId];

		var target = new SearchStateModel();
		target.CopyFrom(source);

		target.CurrentSortCondition.Value.ShouldBe(sortId);
		target.SortDirection.Value.ShouldBe(ListSortDirection.Descending);
		target.CurrentFilteringConditions.Value.ShouldBe([filterId]);
	}

	[Fact]
	public void TabStateModel_ApplyDefaultState_CopiesAllSettingsFromDefaultTab() {
		var mockSp = new Mock<IServiceProvider>();

		var defaultTab = new DefaultTabStateModel(new SearchStateModel(), new ViewerStateModel());
		defaultTab.ActiveRepository.Value = RepositoryType.Album;
		defaultTab.LeftPaneWidth.Value = 320;
		defaultTab.RightPaneWidth.Value = 380;
		defaultTab.RepositoryPaneHeight.Value = 400;
		defaultTab.ViewerState.ItemSize.Value = 220;
		defaultTab.SearchState.SortDirection.Value = ListSortDirection.Descending;

		var newTab = new TabStateModel(mockSp.Object, new SearchStateModel(), new ViewerStateModel());
		newTab.ApplyDefaultState(defaultTab);

		newTab.ActiveRepository.Value.ShouldBe(RepositoryType.Album);
		newTab.LeftPaneWidth.Value.ShouldBe(320);
		newTab.RightPaneWidth.Value.ShouldBe(380);
		newTab.RepositoryPaneHeight.Value.ShouldBe(400);
		newTab.ViewerState.ItemSize.Value.ShouldBe(220);
		newTab.SearchState.SortDirection.Value.ShouldBe(ListSortDirection.Descending);
	}

	[Fact]
	public void DefaultTabStateModel_JsonRoundTrip_PreservesAllSettingsAndColumns() {
		var services = new ServiceCollection();
		services.AddTransient<DefaultTabStateModel>();
		services.AddTransient<SearchStateModel>();
		services.AddTransient<ViewerStateModel>();
		services.AddTransient<ListViewerColumnStateModel>();
		var sp = services.BuildServiceProvider();

		var original = new DefaultTabStateModel(new SearchStateModel(), new ViewerStateModel());
		original.ActiveRepository.Value = RepositoryType.Album;
		original.LeftPaneWidth.Value = 350;
		original.ViewerState.ItemSize.Value = 180;
		var fnCol = original.ViewerState.GetOrCreateColumnState(ListViewerColumnIds.FileName);
		fnCol.Width.Value = 480;
		fnCol.IsVisible.Value = false;

		var jsonDto = DefaultTabStateModelForJson.CreateJson(original);
		var json = JsonSerializer.Serialize(jsonDto, StateJsonSerializerContext.Default.DefaultTabStateModelForJson);

		var deserializedDto = JsonSerializer.Deserialize(json, StateJsonSerializerContext.Default.DefaultTabStateModelForJson);
		deserializedDto.ShouldNotBeNull();

		var restored = DefaultTabStateModelForJson.CreateModel(deserializedDto, sp);
		restored.ShouldNotBeNull();

		restored.ActiveRepository.Value.ShouldBe(RepositoryType.Album);
		restored.LeftPaneWidth.Value.ShouldBe(350);
		restored.ViewerState.ItemSize.Value.ShouldBe(180);
		var restoredFn = restored.ViewerState.ListColumns.FirstOrDefault(x => x.ColumnId == ListViewerColumnIds.FileName);
		restoredFn.ShouldNotBeNull();
		restoredFn.Width.Value.ShouldBe(480);
		restoredFn.IsVisible.Value.ShouldBeFalse();
	}
}