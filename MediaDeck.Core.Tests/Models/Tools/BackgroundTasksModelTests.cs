using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces;
using MediaDeck.Composition.Interfaces.Services;
using MediaDeck.Core.Models.Tools;
using MediaDeck.Core.Services.FileStatusUpdator;
using MediaDeck.Core.Services.MediaItemMetadataUpdator;
using Moq;
using R3;
using Shouldly;

namespace MediaDeck.Core.Tests.Models.Tools;

/// <summary>
/// <see cref="BackgroundTasksModel"/> の単体テスト。
/// </summary>
public sealed class BackgroundTasksModelTests : IDisposable {
	private readonly CompositeDisposable _disposables;
	private readonly BackgroundTasksModel _sut;

	/// <summary>
	/// テストインスタンスを初期化します。
	/// </summary>
	public BackgroundTasksModelTests() {
		(this._sut, _, this._disposables) = CreateSutWithMock();
	}

	/// <summary>
	/// リソースを解放します。
	/// </summary>
	public void Dispose() {
		if (this._sut.DisposeState == DisposeState.NotDisposed) {
			this._sut.Dispose();
		}
		this._disposables.Dispose();
	}

	private static (BackgroundTasksModel Sut, Mock<IFileHashUpdatorService> HashService, CompositeDisposable Disposables) CreateSutWithMock() {
		var disposables = new CompositeDisposable();
		var dbFactory = new Mock<Microsoft.EntityFrameworkCore.IDbContextFactory<MediaDeck.Composition.Database.MediaDeckDbContext>>();
		var hashService = new Mock<IFileHashUpdatorService>();
		var completedCount = new ReactiveProperty<long>().AddTo(disposables);
		var targetCount = new ReactiveProperty<long>().AddTo(disposables);
		var fullHashCompletedCount = new ReactiveProperty<long>().AddTo(disposables);
		var fullHashTargetCount = new ReactiveProperty<long>().AddTo(disposables);
		hashService.Setup(x => x.CompletedCount).Returns(completedCount);
		hashService.Setup(x => x.TargetCount).Returns(targetCount);
		hashService.Setup(x => x.FullHashCompletedCount).Returns(fullHashCompletedCount);
		hashService.Setup(x => x.FullHashTargetCount).Returns(fullHashTargetCount);

		var mediaItemTypeService = new Mock<MediaDeck.Composition.Interfaces.MediaItemTypes.IMediaItemTypeService>();
		var dbWriteCoord = new Mock<IDatabaseWriteCoordinator>();
		var stringProvider = new Mock<IStringProvider>();
		stringProvider.Setup(x => x.GetString("BackgroundTask_UpdateFileStatus")).Returns("Update file status");
		stringProvider.Setup(x => x.GetString("BackgroundTask_UpdateFileHash")).Returns("Update file hash");
		stringProvider.Setup(x => x.GetString("BackgroundTask_UpdateFullHash")).Returns("Update full hash");
		stringProvider.Setup(x => x.GetString("BackgroundTask_UpdateMetadata")).Returns("Update metadata");

		var fileStatusUpdator = new FileStatusUpdatorService(dbFactory.Object, hashService.Object, mediaItemTypeService.Object, dbWriteCoord.Object).AddTo(disposables);
		var metadataUpdator = new MediaItemMetadataUpdatorService(dbFactory.Object, mediaItemTypeService.Object).AddTo(disposables);

		var sut = new BackgroundTasksModel(fileStatusUpdator, hashService.Object, metadataUpdator, stringProvider.Object);
		return (sut, hashService, disposables);
	}

	[Fact]
	public void TaskItems_HasFourItems() {
		this._sut.TaskItems.Count.ShouldBe(4);
	}

	[Fact]
	public void TaskItems_FirstIsUpdateFileStatus() {
		this._sut.TaskItems[0].DisplayName.ShouldBe("Update file status");
	}

	[Fact]
	public void TaskItems_SecondIsUpdateFileHash() {
		this._sut.TaskItems[1].DisplayName.ShouldBe("Update file hash");
	}

	[Fact]
	public void TaskItems_ThirdIsUpdateFullHash() {
		this._sut.TaskItems[2].DisplayName.ShouldBe("Update full hash");
	}

	[Fact]
	public void TaskItems_FourthIsUpdateMetadata() {
		this._sut.TaskItems[3].DisplayName.ShouldBe("Update metadata");
	}

	[Fact]
	public void Actions_IsNotNull() {
		this._sut.Actions.ShouldNotBeNull();
	}

	[Fact]
	public void Dispose_DoesNotThrow() {
		Should.NotThrow(() => this._sut.Dispose());
	}

	/// <summary>
	/// ハッシュ更新タスクの ReRun 実行時に <see cref="IFileHashUpdatorService.EnqueueAllHashUpdatesAsync(CancellationToken)"/> が
	/// <see cref="CancellationToken.None"/> で呼び出されることを検証します。
	/// </summary>
	[Fact]
	public async Task TaskItems_UpdateFileHashReRun_ShouldCallEnqueueAllHashUpdatesAsync() {
		var (sut, hashService, disposables) = CreateSutWithMock();
		using (disposables)
		using (sut) {
			var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
			hashService
				.Setup(x => x.EnqueueAllHashUpdatesAsync(CancellationToken.None))
				.Returns(() => {
					tcs.TrySetResult();
					return Task.CompletedTask;
				});

			sut.TaskItems[1].ReRun();
			await tcs.Task.WaitAsync(TimeSpan.FromSeconds(5));

			tcs.Task.IsCompletedSuccessfully.ShouldBeTrue();
			hashService.Verify(x => x.EnqueueAllHashUpdatesAsync(CancellationToken.None), Times.Once);
		}
	}

	/// <summary>
	/// フルハッシュ更新タスクの ReRun 実行時に <see cref="IFileHashUpdatorService.CheckAndEnqueueFullHashUpdatesAsync(CancellationToken)"/> が
	/// <see cref="CancellationToken.None"/> で呼び出されることを検証します。
	/// </summary>
	[Fact]
	public async Task TaskItems_UpdateFullHashReRun_ShouldCallCheckAndEnqueueFullHashUpdatesAsync() {
		var (sut, hashService, disposables) = CreateSutWithMock();
		using (disposables)
		using (sut) {
			var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
			hashService
				.Setup(x => x.CheckAndEnqueueFullHashUpdatesAsync(CancellationToken.None))
				.Returns(() => {
					tcs.TrySetResult();
					return Task.CompletedTask;
				});

			sut.TaskItems[2].ReRun();
			await tcs.Task.WaitAsync(TimeSpan.FromSeconds(5));

			tcs.Task.IsCompletedSuccessfully.ShouldBeTrue();
			hashService.Verify(x => x.CheckAndEnqueueFullHashUpdatesAsync(CancellationToken.None), Times.Once);
		}
	}
}