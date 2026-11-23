using MediaDeck.Composition.Database;
using MediaDeck.Composition.Interfaces;
using MediaDeck.Composition.Interfaces.MediaItemTypes.ViewModels;
using MediaDeck.Composition.Interfaces.Primitives;
using MediaDeck.Composition.Interfaces.Services;
using MediaDeck.Composition.Interfaces.Threading;
using MediaDeck.Composition.Objects;
using MediaDeck.Composition.Stores.Config.Model;
using MediaDeck.Composition.Stores.State.Model;
using MediaDeck.Core.Models.Files;
using MediaDeck.MediaItemTypes.Pdf.Models;
using MediaDeck.ViewModels.Panes.ViewerPanes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using R3;
using Shouldly;

namespace MediaDeck.Core.Tests.ViewModels;

/// <summary>
/// <see cref="ViewerPaneViewModelBase"/> の単体テスト。
/// </summary>
public sealed class ViewerPaneViewModelBaseTests : IDisposable {
	private readonly SynchronizationContext? _previousSyncContext;
	private readonly InlineSynchronizationContext _syncContext;

	/// <summary>
	/// <see cref="ViewerPaneViewModelBaseTests"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	public ViewerPaneViewModelBaseTests() {
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
	/// 対象アイテムが現在の選択一覧に含まれる場合、選択一覧全体が操作対象として返されることを検証します。
	/// </summary>
	[Fact]
	public void ResolveContextMenuTargetFiles_ReturnsSelectedFiles_WhenTargetIsInSelection() {
		using var env = new TestEnvironment();
		using var vm = env.CreateViewModel();
		var item1 = CreateMediaItem(@"C:\media\a.jpg");
		var item2 = CreateMediaItem(@"C:\media\b.jpg");
		vm.MediaContentLibraryViewModel.SelectedFiles.Value = [item1, item2];

		var result = vm.ResolveContextMenuTargetFiles(item2);

		result.ShouldBe([item1, item2]);
	}

	/// <summary>
	/// 対象アイテムが現在の選択一覧に含まれない場合、対象アイテム単体のみが返されることを検証します。
	/// </summary>
	[Fact]
	public void ResolveContextMenuTargetFiles_ReturnsSingleTarget_WhenTargetIsNotInSelection() {
		using var env = new TestEnvironment();
		using var vm = env.CreateViewModel();
		var item1 = CreateMediaItem(@"C:\media\a.jpg");
		var item2 = CreateMediaItem(@"C:\media\b.jpg");
		var item3 = CreateMediaItem(@"C:\media\c.jpg");
		vm.MediaContentLibraryViewModel.SelectedFiles.Value = [item1, item2];

		var result = vm.ResolveContextMenuTargetFiles(item3);

		result.ShouldBe([item3]);
	}

	/// <summary>
	/// 選択一覧が空の場合、対象アイテム単体が返されることを検証します。
	/// </summary>
	[Fact]
	public void ResolveContextMenuTargetFiles_ReturnsSingleTarget_WhenSelectionIsEmpty() {
		using var env = new TestEnvironment();
		using var vm = env.CreateViewModel();
		var item1 = CreateMediaItem(@"C:\media\a.jpg");

		var result = vm.ResolveContextMenuTargetFiles(item1);

		result.ShouldBe([item1]);
	}

	/// <summary>
	/// 対象アイテムが null の場合、空配列が返されることを検証します。
	/// </summary>
	[Fact]
	public void ResolveContextMenuTargetFiles_ReturnsEmpty_WhenTargetIsNull() {
		using var env = new TestEnvironment();
		using var vm = env.CreateViewModel();
		var item1 = CreateMediaItem(@"C:\media\a.jpg");
		vm.MediaContentLibraryViewModel.SelectedFiles.Value = [item1];

		var result = vm.ResolveContextMenuTargetFiles(null);

		result.ShouldBeEmpty();
	}

	/// <summary>
	/// 単一アイテムのファイルパスをクリップボードへコピーできることを検証します。
	/// </summary>
	[Fact]
	public void CopyFilePaths_CopiesSingleFilePath() {
		using var env = new TestEnvironment();
		using var vm = env.CreateViewModel();
		var item = CreateMediaItem(@"C:\media\single.png");

		vm.CopyFilePaths([item]);

		env.ClipboardMock.Verify(x => x.SetText(@"C:\media\single.png"), Times.Once);
	}

	/// <summary>
	/// 複数アイテムのファイルパスが改行区切りで連結されてクリップボードへコピーされることを検証します。
	/// </summary>
	[Fact]
	public void CopyFilePaths_CopiesMultipleFilePathsJoinedByNewLine() {
		using var env = new TestEnvironment();
		using var vm = env.CreateViewModel();
		var item1 = CreateMediaItem(@"C:\media\one.png");
		var item2 = CreateMediaItem(@"C:\media\two.mp4");

		vm.CopyFilePaths([item1, item2]);

		var expected = string.Join(Environment.NewLine, [@"C:\media\one.png", @"C:\media\two.mp4"]);
		env.ClipboardMock.Verify(x => x.SetText(expected), Times.Once);
	}

	/// <summary>
	/// 空白パスを除外し、大文字・小文字の違いのみの重複パスが排除されてコピーされることを検証します。
	/// </summary>
	[Fact]
	public void CopyFilePaths_IgnoresWhitespaceAndDeduplicatesCaseInsensitively() {
		using var env = new TestEnvironment();
		using var vm = env.CreateViewModel();
		var item1 = CreateMediaItem(@"C:\media\Photo.JPG");
		var item2 = CreateMediaItem("   ");
		var item3 = CreateMediaItem(@"c:\MEDIA\photo.jpg");
		var item4 = CreateMediaItem(string.Empty);
		var item5 = CreateMediaItem(@"C:\media\Second.png");

		vm.CopyFilePaths([item1, item2, item3, item4, item5]);

		var expected = string.Join(Environment.NewLine, [@"C:\media\Photo.JPG", @"C:\media\Second.png"]);
		env.ClipboardMock.Verify(x => x.SetText(expected), Times.Once);
	}

	/// <summary>
	/// null、空一覧、または空白のみのパスしか含まれない場合はクリップボード操作を行わないことを検証します。
	/// </summary>
	[Fact]
	public void CopyFilePaths_DoesNothing_WhenEmptyOrNullOrWhitespaceOnly() {
		using var env = new TestEnvironment();
		using var vm = env.CreateViewModel();
		var blankItem = CreateMediaItem("  \t ");

		vm.CopyFilePaths(null);
		vm.CopyFilePaths([]);
		vm.CopyFilePaths([blankItem]);

		env.ClipboardMock.Verify(x => x.SetText(It.IsAny<string>()), Times.Never);
	}

	private static IMediaItemViewModel CreateMediaItem(string filePath) {
		var mock = new Mock<IMediaItemViewModel>();
		mock.SetupGet(x => x.FilePath).Returns(filePath);
		return mock.Object;
	}

	private sealed class TestViewerPaneViewModel(
		FilesManager filesManager,
		MediaContentLibraryViewModel mediaContentLibraryViewModel,
		SearchConditionManagerViewModel searchConditionManagerViewModel,
		TabStateModel tabState,
		ExecutionConfigModel executionConfig,
		IClipboardService clipboardService)
		: ViewerPaneViewModelBase(
			ViewerType.Wrap,
			"Test",
			"\uE80A",
			filesManager,
			mediaContentLibraryViewModel,
			searchConditionManagerViewModel,
			tabState,
			executionConfig,
			clipboardService);

	private sealed class TestEnvironment : IDisposable {
		private readonly SqliteTestDbFactory _dbFactory = new();
		private readonly StubAppPathProvider _pathProvider = new();
		private readonly ServiceProvider _rootProvider;
		private readonly IServiceScope _scope;

		public TestEnvironment() {
			this.ClipboardMock = new Mock<IClipboardService>();

			var services = new ServiceCollection();
			services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.None));

			global::MediaDeck.Composition.DIRegistration.AddGeneratedServices(services);
			global::MediaDeck.MediaItemTypes.DIRegistration.AddGeneratedServices(services);
			global::MediaDeck.Core.DIRegistration.AddGeneratedServices(services);
			global::MediaDeck.ViewModels.DIRegistration.AddGeneratedServices(services);
			global::MediaDeck.Store.DIRegistration.AddGeneratedServices(services);

			services.AddSingleton<IDbContextFactory<MediaDeckDbContext>>(this._dbFactory);
			services.AddSingleton<IAppPathProvider>(this._pathProvider);
			services.AddSingleton<IStringProvider, StubStringProvider>();

			var uiDispatcherMock = new Mock<IUiDispatcher>();
			uiDispatcherMock.Setup(x => x.Run(It.IsAny<Action>())).Callback<Action>(a => a());
			uiDispatcherMock.Setup(x => x.RunAsync(It.IsAny<Action>())).Returns<Action>(a => {
				a();
				return Task.CompletedTask;
			});
			uiDispatcherMock.Setup(x => x.RunAsync(It.IsAny<Func<Task>>())).Returns<Func<Task>>(async a => await a());
			services.AddSingleton(uiDispatcherMock.Object);

			var locationFactoryMock = new Mock<ILocationFactory>();
			locationFactoryMock.Setup(x => x.Create(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double?>()))
				.Returns<double, double, double?>((lat, lon, alt) => {
					var loc = new Mock<ILocation>();
					loc.Setup(l => l.Latitude).Returns(lat);
					loc.Setup(l => l.Longitude).Returns(lon);
					loc.Setup(l => l.Altitude).Returns(alt);
					return loc.Object;
				});
			services.AddSingleton(locationFactoryMock.Object);

			var appEnvMock = new Mock<IAppEnvironmentService>();
			appEnvMock.Setup(x => x.IsUnpackaged).Returns(true);
			services.AddSingleton(appEnvMock.Object);

			var appUpdateMock = new Mock<IAppUpdateService>();
			appUpdateMock.Setup(x => x.AvailableUpdate).Returns(new ReactiveProperty<AppUpdateInfo?>(null));
			services.AddSingleton(appUpdateMock.Object);

			services.AddSingleton(new Mock<IFilePickerService>().Object);
			services.AddSingleton(this.ClipboardMock.Object);
			services.AddTransient(_ => new Mock<IPdfDocumentOperator>().Object);

			this._rootProvider = services.BuildServiceProvider();
			this._scope = this._rootProvider.CreateScope();
		}

		public Mock<IClipboardService> ClipboardMock {
			get;
		}

		public TestViewerPaneViewModel CreateViewModel() {
			return ActivatorUtilities.CreateInstance<TestViewerPaneViewModel>(this._scope.ServiceProvider);
		}

		public void Dispose() {
			this._scope.Dispose();
			this._rootProvider.Dispose();
			this._dbFactory.Dispose();
			this._pathProvider.Cleanup();
		}
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