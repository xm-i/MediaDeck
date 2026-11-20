using MediaDeck.Composition.Interfaces.Files;
using MediaDeck.Core.Models.NotificationDispatcher;
using R3;
using Shouldly;

namespace MediaDeck.Core.Tests.Models.NotificationDispatcher;

/// <summary>
///     <see cref="SearchConditionNotificationDispatcher"/> のテストクラスです。
/// </summary>
public sealed class SearchConditionNotificationDispatcherTests : IDisposable {
	private readonly SearchConditionNotificationDispatcher _dispatcher = new();

	public void Dispose() {
		this._dispatcher.Dispose();
	}

	/// <summary>
	///     AddRequestにOnNextすると購読者が受信できることを確認します。
	/// </summary>
	[Fact]
	public void AddRequest_EmitsToSubscriber() {
		// Arrange
		ISearchCondition? received = null;
		using var sub = this._dispatcher.AddRequest.Subscribe(x => received = x);
		var mockCondition = new Moq.Mock<ISearchCondition>().Object;

		// Act
		this._dispatcher.AddRequest.OnNext(mockCondition);

		// Assert
		received.ShouldBe(mockCondition);
	}

	/// <summary>
	///     RemoveRequestにOnNextすると購読者が受信できることを確認します。
	/// </summary>
	[Fact]
	public void RemoveRequest_EmitsToSubscriber() {
		// Arrange
		ISearchCondition? received = null;
		using var sub = this._dispatcher.RemoveRequest.Subscribe(x => received = x);
		var mockCondition = new Moq.Mock<ISearchCondition>().Object;

		// Act
		this._dispatcher.RemoveRequest.OnNext(mockCondition);

		// Assert
		received.ShouldBe(mockCondition);
	}

	/// <summary>
	///     SortChangedにOnNextすると購読者が受信できることを確認します。
	/// </summary>
	[Fact]
	public void SortChanged_EmitsToSubscriber() {
		// Arrange
		var received = false;
		using var sub = this._dispatcher.SortChanged.Subscribe(_ => received = true);

		// Act
		this._dispatcher.SortChanged.OnNext(Unit.Default);

		// Assert
		received.ShouldBeTrue();
	}

	/// <summary>
	///     FilterChangedにOnNextすると購読者が受信できることを確認します。
	/// </summary>
	[Fact]
	public void FilterChanged_EmitsToSubscriber() {
		// Arrange
		var received = false;
		using var sub = this._dispatcher.FilterChanged.Subscribe(_ => received = true);

		// Act
		this._dispatcher.FilterChanged.OnNext(Unit.Default);

		// Assert
		received.ShouldBeTrue();
	}

	/// <summary>
	///     ReloadRequestedにOnNextすると購読者が受信できることを確認します。
	/// </summary>
	[Fact]
	public void ReloadRequested_EmitsToSubscriber() {
		// Arrange
		var received = false;
		using var sub = this._dispatcher.ReloadRequested.Subscribe(_ => received = true);

		// Act
		this._dispatcher.ReloadRequested.OnNext(Unit.Default);

		// Assert
		received.ShouldBeTrue();
	}

	/// <summary>
	///     UpdateRequestにOnNextすると購読者が受信できることを確認します。
	/// </summary>
	[Fact]
	public void UpdateRequest_EmitsToSubscriber() {
		// Arrange
		var received = false;
		using var sub = this._dispatcher.UpdateRequest.Subscribe(_ => received = true);

		// Act
		this._dispatcher.UpdateRequest.OnNext(_ => { });

		// Assert
		received.ShouldBeTrue();
	}

	/// <summary>
	///     SearchRequestedストリームが構築されることを確認します。
	/// </summary>
	[Fact]
	public void SearchRequested_IsNotNull() {
		this._dispatcher.SearchRequested.ShouldNotBeNull();
	}

	/// <summary>
	///     Dispose後にSubjectが正常に処理されることを確認します。
	/// </summary>
	[Fact]
	public void Dispose_DisposesAllSubjects() {
		// Arrange
		var dispatcher = new SearchConditionNotificationDispatcher();

		// Act
		dispatcher.Dispose();

		// Assert — Disposed後にOnNextすると例外が発生する
		Should.Throw<ObjectDisposedException>(() =>
			dispatcher.AddRequest.OnNext(new Moq.Mock<ISearchCondition>().Object));
		Should.Throw<ObjectDisposedException>(() =>
			dispatcher.SearchConditionChanged.OnNext(Unit.Default));
	}
}