using MediaDeck.Composition.Stores.State.Model.Objects;
using MediaDeck.Core.Models.Files.Filter;
using MediaDeck.Core.Models.Files.Filter.FilterItemObjects;
using R3;
using Shouldly;

namespace MediaDeck.Core.Tests.Models.Files.Filter;

public sealed class FilteringConditionTests : IDisposable {
	private readonly FilterObject _filterObject = new() { DisplayName = { Value = "TestFilter" } };
	private readonly FilteringCondition _sut;

	public FilteringConditionTests() {
		this._sut = new FilteringCondition(this._filterObject);
	}

	public void Dispose() {
		this._sut.Dispose();
	}

	[Fact]
	public void DisplayName_ReflectsFilterObject() {
		this._sut.DisplayName.CurrentValue.ShouldBe("TestFilter");
	}

	[Fact]
	public void FilterItemObjects_IsInitiallyEmpty() {
		this._sut.FilterItemObjects.Count.ShouldBe(0);
	}

	[Fact]
	public void FilterObject_ReturnsSameInstance() {
		this._sut.FilterObject.ShouldBeSameAs(this._filterObject);
	}

	[Fact]
	public void OnUpdateFilteringConditions_EmitsWhenItemAdded() {
		var emitted = false;
		using var sub = this._sut.OnUpdateFilteringConditions.Subscribe(_ => emitted = true);

		this._filterObject.FilterItemObjects.Add(new ExistsFilterItemObject { Exists = true });

		emitted.ShouldBeTrue();
	}

	[Fact]
	public void OnUpdateFilteringConditions_EmitsWhenItemRemoved() {
		var item = new ExistsFilterItemObject { Exists = true };
		this._filterObject.FilterItemObjects.Add(item);

		var emitted = false;
		using var sub = this._sut.OnUpdateFilteringConditions.Subscribe(_ => emitted = true);

		this._filterObject.FilterItemObjects.Remove(item);

		emitted.ShouldBeTrue();
	}
}