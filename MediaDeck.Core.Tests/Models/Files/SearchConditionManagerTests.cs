using MediaDeck.Composition.Database;
using MediaDeck.Composition.Interfaces.Files;
using MediaDeck.Composition.Interfaces.Notifications;
using MediaDeck.Composition.Interfaces.Tags;
using MediaDeck.Composition.Stores.State.Model;
using MediaDeck.Core.Models.Files;
using MediaDeck.Core.Models.Files.SearchConditions;
using MediaDeck.Core.Models.Repositories;
using MediaDeck.Core.Models.Repositories.Objects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using ObservableCollections;
using R3;
using Shouldly;

namespace MediaDeck.Core.Tests.Models.Files;

public class SearchConditionManagerTests {
	private static IDbContextFactory<MediaDeckDbContext> CreateInMemoryDbFactory(string dbName) {
		var options = new DbContextOptionsBuilder<MediaDeckDbContext>()
			.UseInMemoryDatabase(databaseName: dbName)
			.ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
			.Options;

		var factoryMock = new Mock<IDbContextFactory<MediaDeckDbContext>>();
		factoryMock.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(() => new MediaDeckDbContext(options));

		return factoryMock.Object;
	}

	private static SearchConditionManagerTestContext CreateSut(string testName) {
		var addSubject = new Subject<ISearchCondition>();
		var removeSubject = new Subject<ISearchCondition>();
		var updateSubject = new Subject<Action<ObservableList<ISearchCondition>>>();

		var dispatcherMock = new Mock<ISearchConditionNotificationDispatcher>();
		dispatcherMock.Setup(x => x.AddRequest).Returns(addSubject);
		dispatcherMock.Setup(x => x.RemoveRequest).Returns(removeSubject);
		dispatcherMock.Setup(x => x.UpdateRequest).Returns(updateSubject);

		var tagsManagerMock = new Mock<ITagsManager>();
		tagsManagerMock.Setup(x => x.Tags).Returns(new ObservableList<ITagModel>());

		// FolderRepositoryにInMemory DBを渡して空のルートフォルダを返す状態にする
		var dbFactory = CreateInMemoryDbFactory(testName);
		var folderDispatcherMock = new Mock<ISearchConditionNotificationDispatcher>();
		var folderUpdateSubject = new Subject<Action<ObservableList<ISearchCondition>>>();
		folderDispatcherMock.Setup(x => x.UpdateRequest).Returns(folderUpdateSubject);
		var services = new ServiceCollection();
		MediaDeck.Composition.DIRegistration.AddGeneratedServices(services);
		var tabState = services.BuildServiceProvider().GetRequiredService<TabStateModel>();
		var folderRepo = new FolderRepository(dbFactory, folderDispatcherMock.Object, tabState);
		folderRepo.RootFolder.Value = new FolderObject(null, "", []);

		var manager = new SearchConditionManager(dispatcherMock.Object, tagsManagerMock.Object, folderRepo, tabState);
		return new SearchConditionManagerTestContext(manager, folderRepo, addSubject, removeSubject, updateSubject, folderUpdateSubject);
	}

	/// <summary>
	///     AddRequestで検索条件が追加されることを確認します。
	/// </summary>
	[Fact]
	public void AddRequest_AddsSearchCondition() {
		using var context = CreateSut(nameof(AddRequest_AddsSearchCondition));

		var condition = new WordSearchCondition { Word = "test" };
		context.AddSubject.OnNext(condition);

		context.Manager.SearchConditions.ShouldContain(condition);
	}

	/// <summary>
	///     RemoveRequestで検索条件が削除されることを確認します。
	/// </summary>
	[Fact]
	public void RemoveRequest_RemovesSearchCondition() {
		using var context = CreateSut(nameof(RemoveRequest_RemovesSearchCondition));

		var condition = new WordSearchCondition { Word = "test" };
		context.AddSubject.OnNext(condition);
		context.RemoveSubject.OnNext(condition);

		context.Manager.SearchConditions.ShouldNotContain(condition);
	}

	/// <summary>
	///     UpdateRequestでリストを操作できることを確認します。
	/// </summary>
	[Fact]
	public void UpdateRequest_ModifiesSearchConditions() {
		using var context = CreateSut(nameof(UpdateRequest_ModifiesSearchConditions));

		var condition = new WordSearchCondition { Word = "injected" };
		context.UpdateSubject.OnNext(list => list.Add(condition));

		context.Manager.SearchConditions.ShouldContain(condition);
	}

	/// <summary>
	///     SearchConditionCandidatesにPropertySearchConditionのサジェストが含まれることを確認します。
	/// </summary>
	[Fact]
	public void Candidates_ContainPropertySearchConditions() {
		using var context = CreateSut(nameof(Candidates_ContainPropertySearchConditions));

		context.Manager.SearchConditionCandidates.OfType<PropertySearchCondition>().ShouldNotBeEmpty();
	}

	private sealed class SearchConditionManagerTestContext(
		SearchConditionManager manager,
		FolderRepository folderRepository,
		Subject<ISearchCondition> addSubject,
		Subject<ISearchCondition> removeSubject,
		Subject<Action<ObservableList<ISearchCondition>>> updateSubject,
		Subject<Action<ObservableList<ISearchCondition>>> folderUpdateSubject) : IDisposable {
		public SearchConditionManager Manager {
			get;
		} = manager;

		public Subject<ISearchCondition> AddSubject {
			get;
		} = addSubject;

		public Subject<ISearchCondition> RemoveSubject {
			get;
		} = removeSubject;

		public Subject<Action<ObservableList<ISearchCondition>>> UpdateSubject {
			get;
		} = updateSubject;

		private FolderRepository FolderRepository {
			get;
		} = folderRepository;

		private Subject<Action<ObservableList<ISearchCondition>>> FolderUpdateSubject {
			get;
		} = folderUpdateSubject;

		public void Dispose() {
			this.Manager.Dispose();
			this.FolderRepository.Dispose();
			this.AddSubject.Dispose();
			this.RemoveSubject.Dispose();
			this.UpdateSubject.Dispose();
			this.FolderUpdateSubject.Dispose();
		}
	}
}