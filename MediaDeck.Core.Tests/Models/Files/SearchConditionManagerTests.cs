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

		var tags = new ObservableList<ITagModel>();
		var tagsManagerMock = new Mock<ITagsManager>();
		tagsManagerMock.Setup(x => x.Tags).Returns(tags);

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
		return new SearchConditionManagerTestContext(manager, folderRepo, tags, addSubject, removeSubject, updateSubject, folderUpdateSubject);
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

	/// <summary>
	///     タグ追加時に候補が更新され、さらにタグ内容編集後も最新のDisplayText・サジェスト判定に追従することを確認します。
	/// </summary>
	[Fact]
	public void Candidates_ReflectAddedAndEditedTag() {
		using var context = CreateSut(nameof(Candidates_ReflectAddedAndEditedTag));
		using var tag = new MediaDeck.Core.Models.Tags.TagModel {
			TagId = 42,
			TagName = string.Empty
		};

		// 新規タグ追加（空名）
		context.Tags.Add(tag);
		var tagCandidate = context.Manager.SearchConditionCandidates
			.OfType<TagSearchCondition>()
			.FirstOrDefault(x => x.TagId == 42);
		tagCandidate.ShouldNotBeNull();
		tagCandidate.DisplayText.ShouldBe("TagName=");

		// 編集後（タグ名と別名を更新）
		tag.TagName = "風景";
		tag.Ruby = "ふうけい";
		using var alias = new MediaDeck.Core.Models.Tags.TagAliasModel {
			Alias = "絶景",
			Ruby = "ぜっけい"
		};
		tag.TagAliases = [alias];

		tagCandidate.IsMatchForSuggest("fuukei").ShouldBeTrue();
		tagCandidate.DisplayText.ShouldBe("TagName=風景");

		tagCandidate.IsMatchForSuggest("zekkei").ShouldBeTrue();
		tagCandidate.DisplayText.ShouldBe("TagName=風景 (絶景)");

		// 再度タグ名一致に戻ると代表テキストが解除される
		tagCandidate.IsMatchForSuggest("風景").ShouldBeTrue();
		tagCandidate.DisplayText.ShouldBe("TagName=風景");

		// タグ削除で候補からも除去される
		context.Tags.Remove(tag);
		context.Manager.SearchConditionCandidates.OfType<TagSearchCondition>().ShouldNotContain(x => x.TagId == 42);
	}

	private sealed class SearchConditionManagerTestContext(
		SearchConditionManager manager,
		FolderRepository folderRepository,
		ObservableList<ITagModel> tags,
		Subject<ISearchCondition> addSubject,
		Subject<ISearchCondition> removeSubject,
		Subject<Action<ObservableList<ISearchCondition>>> updateSubject,
		Subject<Action<ObservableList<ISearchCondition>>> folderUpdateSubject) : IDisposable {
		public SearchConditionManager Manager {
			get;
		} = manager;

		public ObservableList<ITagModel> Tags {
			get;
		} = tags;

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