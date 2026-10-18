using MediaDeck.Composition.Interfaces.Notifications;
using MediaDeck.Composition.Stores.Config.Model;
using MediaDeck.Composition.Stores.State.Model;
using MediaDeck.Composition.Stores.State.Model.Objects;
using MediaDeck.Core.Models.Files.Filter;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using R3;
using Shouldly;

namespace MediaDeck.Core.Tests.Models.Files.Filter;

public class FilterSelectorTests : IDisposable {
	private readonly Mock<ISearchConditionNotificationDispatcher> _dispatcherMock;
	private readonly SearchDefinitionsConfigModel _searchDefinitions;
	private readonly FilterSelector _sut;

	public FilterSelectorTests() {
		this._dispatcherMock = new Mock<ISearchConditionNotificationDispatcher>();
		this._dispatcherMock.Setup(x => x.FilterChanged).Returns(new Subject<Unit>());
		var spMock = new Mock<IServiceProvider>();
		spMock.Setup(x => x.GetService(typeof(SortObject))).Returns(() => new SortObject(spMock.Object));
		this._searchDefinitions = new SearchDefinitionsConfigModel(spMock.Object, new StubStringProvider());
		var services = new ServiceCollection();
		MediaDeck.Composition.DIRegistration.AddGeneratedServices(services);
		var tabState = services.BuildServiceProvider().GetRequiredService<TabStateModel>();

		this._sut = new FilterSelector(tabState, this._searchDefinitions, this._dispatcherMock.Object);
	}

	public void Dispose() {
		this._sut.Dispose();
	}

	[Fact]
	public void CurrentFilteringConditions_DefaultIsEmpty() {
		this._sut.CurrentFilteringConditions.Value.ShouldBeEmpty();
	}

	[Fact]
	public void FilteringConditions_DefaultIsEmpty() {
		this._sut.FilteringConditions.Count.ShouldBe(0);
	}
}