using MediaDeck.Composition.Interfaces.Notifications;
using MediaDeck.Composition.Interfaces.Services;
using MediaDeck.Composition.Interfaces.Tags;
using MediaDeck.Core.Models.Files;
using MediaDeck.Core.Primitives;
using MediaDeck.ViewModels.Panes.DetailPanes;
using MediaDeck.ViewModels.Tags;
using Moq;
using ObservableCollections;
using R3;

namespace MediaDeck.Core.Tests.ViewModels;

/// <summary>
/// <see cref="TagsDetailViewModel"/> の単体テスト。
/// </summary>
public sealed class TagsDetailViewModelTests : IDisposable {
	private readonly SynchronizationContext? _previousSyncContext;
	private readonly InlineSynchronizationContext _syncContext;

	/// <summary>
	/// <see cref="TagsDetailViewModelTests"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	public TagsDetailViewModelTests() {
		this._previousSyncContext = SynchronizationContext.Current;
		this._syncContext = new InlineSynchronizationContext();
		SynchronizationContext.SetSynchronizationContext(this._syncContext);
	}

	/// <summary>
	/// テスト終了時に同期コンテキストを元の状態へ復元します。
	/// </summary>
	public void Dispose() {
		SynchronizationContext.SetSynchronizationContext(this._previousSyncContext);
	}

	/// <summary>
	/// タグ名コピーコマンドを実行すると、対象タグ名がクリップボードサービスへ渡されることを検証します。
	/// </summary>
	[Fact]
	public void CopyTagNameCommand_CopiesTagNameToClipboard() {
		using var disposables = new CompositeDisposable();
		var clipboardMock = new Mock<IClipboardService>();
		var (vm, tagsManagerMock, tagModelFactoryMock) = CreateViewModel(clipboardMock.Object, disposables);
		var tagVm = CreateTagViewModel("風景", tagsManagerMock.Object, tagModelFactoryMock.Object, disposables);

		vm.CopyTagNameCommand.Execute(new ValueCountPair<TagViewModel>(tagVm, 3));

		clipboardMock.Verify(x => x.SetText("風景"), Times.Once);
	}

	/// <summary>
	/// タグ名が空文字の場合、タグ名コピーコマンドがクリップボードサービスを呼び出さないことを検証します。
	/// </summary>
	[Fact]
	public void CopyTagNameCommand_DoesNothing_WhenTagNameIsEmpty() {
		using var disposables = new CompositeDisposable();
		var clipboardMock = new Mock<IClipboardService>();
		var (vm, tagsManagerMock, tagModelFactoryMock) = CreateViewModel(clipboardMock.Object, disposables);
		var tagVm = CreateTagViewModel(string.Empty, tagsManagerMock.Object, tagModelFactoryMock.Object, disposables);

		vm.CopyTagNameCommand.Execute(new ValueCountPair<TagViewModel>(tagVm, 1));

		clipboardMock.Verify(x => x.SetText(It.IsAny<string>()), Times.Never);
	}

	/// <summary>
	/// ペアの値が null（既定値）の場合、例外を投げずにクリップボードサービスを呼び出さないことを検証します。
	/// </summary>
	[Fact]
	public void CopyTagNameCommand_DoesNothing_WhenPairValueIsNull() {
		using var disposables = new CompositeDisposable();
		var clipboardMock = new Mock<IClipboardService>();
		var (vm, _, _) = CreateViewModel(clipboardMock.Object, disposables);

		vm.CopyTagNameCommand.Execute(default);

		clipboardMock.Verify(x => x.SetText(It.IsAny<string>()), Times.Never);
	}

	private static (TagsDetailViewModel ViewModel, Mock<ITagsManager> TagsManagerMock, Mock<ITagModelFactory> TagModelFactoryMock) CreateViewModel(
		IClipboardService clipboardService,
		CompositeDisposable disposables) {
		var tagsManagerMock = new Mock<ITagsManager>();
		tagsManagerMock.Setup(x => x.Tags).Returns(new ObservableList<ITagModel>());
		tagsManagerMock.Setup(x => x.TagCategories).Returns(new ObservableList<ITagCategoryModel>());

		var model = new TagsDetailModel(tagsManagerMock.Object).AddTo(disposables);
		var notificationDispatcherMock = new Mock<ISearchConditionNotificationDispatcher>();
		var tagModelFactoryMock = new Mock<ITagModelFactory>();

		var vm = new TagsDetailViewModel(
			model,
			notificationDispatcherMock.Object,
			tagModelFactoryMock.Object,
			clipboardService).AddTo(disposables);

		return (vm, tagsManagerMock, tagModelFactoryMock);
	}

	private static TagViewModel CreateTagViewModel(
		string tagName,
		ITagsManager tagsManager,
		ITagModelFactory tagModelFactory,
		CompositeDisposable disposables) {
		var usageCount = new ReactiveProperty<int>(1).AddTo(disposables);
		var tagMock = new Mock<ITagModel>();
		tagMock.SetupGet(x => x.TagName).Returns(tagName);
		tagMock.SetupGet(x => x.Detail).Returns(string.Empty);
		tagMock.SetupGet(x => x.UsageCount).Returns(usageCount);
		tagMock.SetupGet(x => x.TagAliases).Returns([]);

		return new TagViewModel(null, tagMock.Object, tagsManager, tagModelFactory).AddTo(disposables);
	}

	private sealed class InlineSynchronizationContext : SynchronizationContext {
		public override void Post(SendOrPostCallback d, object? state) {
			d(state);
		}

		public override void Send(SendOrPostCallback d, object? state) {
			d(state);
		}
	}
}