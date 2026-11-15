using MediaDeck.Composition.Interfaces.MediaItemTypes;
using MediaDeck.Composition.Stores.Config.Model;
using MediaDeck.Composition.Stores.Config.Model.Objects;
using MediaDeck.Composition.Stores.State.Model.Objects;
using MediaDeck.Core.Models.Files;

using Microsoft.Extensions.DependencyInjection;

using Moq;

using Shouldly;

namespace MediaDeck.Core.Tests.Models.Files;

public class FileRegistrarTests {
	private readonly FileRegistrar _sut;
	private readonly ConfigModel _config;

	public FileRegistrarTests() {
		var services = new ServiceCollection();
		services.AddSingleton<PathConfigModel>();
		services.AddSingleton<ExecutionConfigModel>();
		services.AddTransient<ExtensionObjectModel>();
		services.AddSingleton<ScanConfigModel>();
		services.AddSingleton<ThumbnailConfigModel>();
		services.AddSingleton<SearchConfigModel>();
		services.AddSingleton<EnvironmentConfigModel>();
		services.AddSingleton<FolderManagerConfigModel>();
		services.AddSingleton<SearchDefinitionsConfigModel>();
		services.AddSingleton<LanguageConfigModel>();
		services.AddSingleton<MediaDeck.Composition.Interfaces.IStringProvider, StubStringProvider>();
		services.AddSingleton<ConfigModel>();
		services.AddTransient<SortObject>(sp => new SortObject(sp));
		services.AddTransient<SortItemObject>();
		var provider = services.BuildServiceProvider();

		this._config = provider.GetRequiredService<ConfigModel>();
		var logger = new Mock<Microsoft.Extensions.Logging.ILogger<FileRegistrar>>();
		var mediaItemTypeService = new Mock<IMediaItemTypeService>();
		mediaItemTypeService.Setup(x => x.CreateMediaItemOperators()).Returns([]);

		this._sut = new FileRegistrar(this._config, logger.Object, mediaItemTypeService.Object);
	}

	[Fact]
	public void Config_ReturnsSameInstance() {
		this._sut.Config.ShouldBeSameAs(this._config);
	}

	[Fact]
	public void RegistrationQueue_InitiallyEmpty() {
		this._sut.RegistrationQueue.Count.ShouldBe(0);
	}

	[Fact]
	public void Enqueue_AddsItemToQueue() {
		this._sut.Enqueue("file1.png");
		this._sut.RegistrationQueue.Count.ShouldBe(1);
		this._sut.TryDequeue(out var dequeued).ShouldBeTrue();
		dequeued.ShouldBe("file1.png");
	}

	[Fact]
	public void EnqueueRange_AddsItemsToQueue() {
		this._sut.EnqueueRange(["file1.png", "file2.png"]);
		this._sut.RegistrationQueue.Count.ShouldBe(2);
	}

	[Fact]
	public async Task ScanFolderAsync_InvalidDirectory_ResetsIsScanningToFalse() {
		var nonExistentPath = Path.Combine(Path.GetTempPath(), $"NonExistentPath_{Guid.NewGuid()}");
		var folder = new FolderModel { FolderPath = nonExistentPath };
		await Should.ThrowAsync<DirectoryNotFoundException>(async () => {
			await this._sut.ScanFolderAsync(folder);
		});
		folder.IsScanning.Value.ShouldBeFalse();
	}

	[Fact]
	public async Task ScanFolderAsync_ValidEmptyFolder_CompletesAndSetsIsScanningToFalse() {
		var tempDir = Path.Combine(Path.GetTempPath(), $"MD_Test_{Guid.NewGuid()}");
		Directory.CreateDirectory(tempDir);
		try {
			var folder = new FolderModel { FolderPath = tempDir };
			await this._sut.ScanFolderAsync(folder);
			folder.IsScanning.Value.ShouldBeFalse();
			folder.TotalCount.Value.ShouldBe(0);
			folder.RemainingCount.Value.ShouldBe(0);
		} finally {
			Directory.Delete(tempDir, true);
		}
	}

	[Fact]
	public async Task ScanFolderAsync_FolderWithFiles_SetsTotalAndRemainingCount() {
		var tempDir = Path.Combine(Path.GetTempPath(), $"MD_Test_{Guid.NewGuid()}");
		Directory.CreateDirectory(tempDir);
		var file1 = Path.Combine(tempDir, "test1.png");
		var file2 = Path.Combine(tempDir, "test2.jpg");
		File.WriteAllText(file1, "dummy");
		File.WriteAllText(file2, "dummy");

		var mediaItemTypeService = new Mock<IMediaItemTypeService>();
		mediaItemTypeService.Setup(x => x.CreateMediaItemOperators()).Returns([]);
		mediaItemTypeService.Setup(x => x.IsTargetPath(It.IsAny<string>())).Returns(true);

		var sut = new FileRegistrar(this._config, new Mock<Microsoft.Extensions.Logging.ILogger<FileRegistrar>>().Object, mediaItemTypeService.Object);
		try {
			var folder = new FolderModel { FolderPath = tempDir };
			await sut.ScanFolderAsync(folder);

			// バックグラウンドでの登録処理完了を待機
			var timeout = DateTime.UtcNow.AddSeconds(5);
			while (folder.IsScanning.Value && DateTime.UtcNow < timeout) {
				await Task.Delay(50);
			}

			folder.IsScanning.Value.ShouldBeFalse();
			folder.TotalCount.Value.ShouldBe(2);
			folder.RemainingCount.Value.ShouldBe(0);
		} finally {
			sut.Dispose();
			Directory.Delete(tempDir, true);
		}
	}

	[Fact]
	public void Dispose_DoesNotThrow() {
		Should.NotThrow(() => this._sut.Dispose());
	}
}