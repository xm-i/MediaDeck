using MediaDeck.Common.Base;
using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.MediaItemTypes;
using MediaDeck.Composition.Interfaces.MediaItemTypes.Models;
using MediaDeck.Composition.Interfaces.MediaItemTypes.ViewModels;
using MediaDeck.Composition.Interfaces.MediaItemTypes.Views;
using MediaDeck.Composition.Interfaces.Primitives;
using MediaDeck.Composition.Interfaces.Tags;
using MediaDeck.Composition.Objects;
using MediaDeck.Core.Services;
using MediaDeck.Database.Tables;
using R3;
using Shouldly;

namespace MediaDeck.Core.Tests.Services;

/// <summary>
/// <see cref="MediaItemTypeService" /> のテストクラス。
/// </summary>
public class MediaItemTypeServiceTest {
	/// <summary>
	/// Unknown用のテストファクトリ。
	/// </summary>
	private static readonly TestMediaItemFactory UnknownMediaItemFactory = new(MediaType.Unknown, "unknown");

	/// <summary>
	/// Image用のテストファクトリ。
	/// </summary>
	private static readonly TestMediaItemFactory ImageMediaItemFactory = new(MediaType.Image, "image");

	/// <summary>
	/// Video用のテストファクトリ。
	/// </summary>
	private static readonly TestMediaItemFactory VideoMediaItemFactory = new(MediaType.Video, "video");

	/// <summary>
	/// Unknown用のテストプロバイダー。
	/// </summary>
	private static readonly TestMediaItemTypeProvider UnknownMediaItemProvider = new(MediaType.Unknown, "unknown");

	/// <summary>
	/// Image用のテストプロバイダー。
	/// </summary>
	private static readonly TestMediaItemTypeProvider ImageMediaItemProvider = new(MediaType.Image, "image");

	/// <summary>
	/// Video用のテストプロバイダー。
	/// </summary>
	private static readonly TestMediaItemTypeProvider VideoMediaItemProvider = new(MediaType.Video, "video");

	private readonly MediaItemTypeService _service;

	public MediaItemTypeServiceTest() {
		var factories = new[] { UnknownMediaItemFactory, ImageMediaItemFactory, VideoMediaItemFactory };
		var providers = new[] { UnknownMediaItemProvider, ImageMediaItemProvider, VideoMediaItemProvider };
		this._service = new MediaItemTypeService(factories, providers);
	}

	/// <summary>
	/// 画像ファイルのレコードから画像用モデルが生成されることを確認する。
	/// </summary>
	[Fact]
	public void CreateMediaItemModelFromRecord_UsesMatchingMediaItemType() {
		var MediaItem = CreateMediaItem(@"C:\media\sample.jpg");

		var result = this._service.CreateMediaItemModelFromRecord(MediaItem);

		var testFileModel = result.ShouldBeOfType<TestFileModel>();
		testFileModel.CreatedBy.ShouldBe("image");
	}

	/// <summary>
	/// 未登録の拡張子はUnknown用モデルにフォールバックすることを確認する。
	/// </summary>
	[Fact]
	public void CreateMediaItemModelFromRecord_FallsBackToUnknownWhenMediaTypeIsNotRegistered() {
		var MediaItem = CreateMediaItem(@"C:\media\sample.txt");

		var result = this._service.CreateMediaItemModelFromRecord(MediaItem);

		var testFileModel = result.ShouldBeOfType<TestFileModel>();
		testFileModel.CreatedBy.ShouldBe("unknown");
	}

	/// <summary>
	/// ファイルモデルのメディアタイプに対応するビューモデルが生成されることを確認する。
	/// </summary>
	[Fact]
	public void CreateMediaItemViewModel_UsesMatchingMediaItemType() {
		var fileModel = new TestFileModel(MediaType.Video, @"C:\media\sample.mp4", "input");

		var result = this._service.CreateMediaItemViewModel(fileModel);

		var testFileViewModel = result.ShouldBeOfType<TestFileViewModel>();
		testFileViewModel.CreatedBy.ShouldBe("video");
	}

	/// <summary>
	/// 未登録のメディアタイプを持つビューモデルではUnknown用プレビューが生成されることを確認する。
	/// </summary>
	[Fact]
	public void CreateDetailViewerPreviewControlView_FallsBackToUnknownWhenMediaTypeIsNotRegistered() {
		var fileViewModel = new TestFileViewModel(new TestFileModel(MediaType.Pdf, @"C:\media\sample.pdf", "input"), "input");

		var result = this._service.CreateDetailViewerPreviewControlView(fileViewModel);

		var testView = result.ShouldBeOfType<TestDetailViewerPreviewControlView>();
		testView.CreatedBy.ShouldBe("unknown");
	}

	/// <summary>
	/// サムネイル関連のファクトリが対応するファイルタイプを使用することを確認する。
	/// </summary>
	[Fact]
	public void ThumbnailFactories_UseMatchingMediaItemType() {
		var fileViewModel = new TestFileViewModel(new TestFileModel(MediaType.Video, @"C:\media\sample.mp4", "input"), "input");

		var thumbnailPickerViewModel = this._service.CreateThumbnailPickerViewModel(fileViewModel);
		var thumbnailPickerView = this._service.CreateThumbnailPickerView(fileViewModel);

		var testPickerVm = thumbnailPickerViewModel.ShouldBeOfType<TestThumbnailPickerViewModel>();
		testPickerVm.CreatedBy.ShouldBe("video");
		var testPickerView = thumbnailPickerView.ShouldBeOfType<TestThumbnailPickerView>();
		testPickerView.CreatedBy.ShouldBe("video");
	}

	/// <summary>
	/// 登録済みのすべてのファイルタイプからファイルオペレーターが生成されることを確認する。
	/// </summary>
	[Fact]
	public void CreateMediaItemOperators_ReturnsOperatorsForAllRegisteredMediaItemTypes() {
		var result = this._service.CreateMediaItemOperators();

		result.Count().ShouldBe(3);
		result.ShouldAllBe(x => x is TestFileOperator);
		result.Cast<TestFileOperator>().Select(x => x.CreatedBy).ToArray().ShouldBe(["unknown", "image", "video"]);
	}

	/// <summary>
	/// IncludeTablesが登録済みのすべてのファイルタイプに順番に委譲されることを確認する。
	/// </summary>
	[Fact]
	public void IncludeTables_AppliesAllRegisteredMediaItemTypes() {
		var MediaItems = new[] { CreateMediaItem(@"C:\media\base.dat") }.AsQueryable();

		var result = this._service.IncludeTables(MediaItems).ToList();

		result.Select(x => x.FilePath).ToArray()
			.ShouldBe([@"C:\media\base.dat",
				@"C:\included\unknown.dat",
				@"C:\included\image.dat",
				@"C:\included\video.dat"]);
	}

	[Fact]
	public void GetMediaItemType_ByPath_UsesMatchingType() {
		var result = this._service.GetMediaItemFactory(@"C:\media\sample.jpg");

		result.ShouldBeSameAs(ImageMediaItemFactory);
	}

	[Fact]
	public void GetMediaItemType_ByMediaItem_UsesItemTypeMapping() {
		var mediaItem = CreateMediaItem(@"C:\media\sample.mp4");

		var result = this._service.GetMediaItemFactory(mediaItem);

		result.ShouldBeSameAs(VideoMediaItemFactory);
	}

	[Theory]
	[InlineData(@"C:\media\sample.jpg", true)]
	[InlineData(@"C:\media\sample.mp4", true)]
	[InlineData(@"C:\media\sample.pdf", false)]
	public void IsTargetPath_ShouldReturnExpectedResult(string path, bool expected) {
		var result = this._service.IsTargetPath(path);

		result.ShouldBe(expected);
	}

	[Theory]
	[InlineData(@"C:\media\sample.jpg", MediaType.Image, true)]
	[InlineData(@"C:\media\sample.jpg", MediaType.Video, false)]
	[InlineData(@"C:\media\sample.mp4", MediaType.Video, true)]
	public void IsTargetPath_WithMediaType_ShouldReturnExpectedResult(string path, MediaType mediaType, bool expected) {
		var result = this._service.IsTargetPath(path, mediaType);

		result.ShouldBe(expected);
	}

	/// <summary>
	/// テスト用の <see cref="MediaItem" /> を生成する。
	/// </summary>
	/// <param name="filePath">ファイルパス。</param>
	/// <returns>生成した <see cref="MediaItem" />。</returns>
	private static MediaItem CreateMediaItem(string filePath) {
		var itemType = Path.GetExtension(filePath).ToLowerInvariant() switch {
			".jpg" => ItemType.Image,
			".mp4" => ItemType.Video,
			_ => ItemType.Unknown
		};
		return new MediaItem { DirectoryPath = Path.GetDirectoryName(filePath) ?? string.Empty, FilePath = filePath, Description = string.Empty, MediaItemTags = new List<MediaItemTag>(), ItemType = itemType, IsUnderFolderGroup = false };
	}

	private sealed class TestMediaItemFactory : IMediaItemFactory {
		public TestMediaItemFactory(MediaType mediaType, string createdBy) {
			this.MediaType = mediaType;
			this.CreatedBy = createdBy;
		}

		public MediaType MediaType {
			get;
		}

		public ItemType ItemType {
			get {
				return this.MediaType switch {
					MediaType.Image => ItemType.Image,
					MediaType.Video => ItemType.Video,
					_ => ItemType.Unknown
				};
			}
		}

		public string CreatedBy {
			get;
		}

		public IMediaItemOperator CreateMediaItemOperator() {
			return new TestFileOperator(this.MediaType, this.CreatedBy);
		}

		public IMediaItemModel CreateMediaItemModelFromRecord(MediaItem MediaItem) {
			return new TestFileModel(this.MediaType, MediaItem.FilePath, this.CreatedBy);
		}

		public IDetailViewerPreviewControlView CreateDetailViewerPreviewControlView(IMediaItemViewModel fileViewModel) {
			return new TestDetailViewerPreviewControlView(this.CreatedBy) { DataContext = fileViewModel };
		}

		public IThumbnailControlView CreateThumbnailControlView(IMediaItemViewModel fileViewModel) {
			return new TestThumbnailControlView(this.CreatedBy) { DataContext = fileViewModel };
		}

		public IMediaItemViewModel CreateMediaItemViewModel(IMediaItemModel fileModel) {
			return new TestFileViewModel(fileModel, this.CreatedBy);
		}

		public IThumbnailPickerViewModel CreateThumbnailPickerViewModel() {
			return new TestThumbnailPickerViewModel(this.CreatedBy);
		}

		public IThumbnailPickerView CreateThumbnailPickerView() {
			return new TestThumbnailPickerView(this.CreatedBy);
		}

		public IExecutionProgramObjectModel CreateExecutionProgramObjectModel() {
			return null!;
		}

		public IExecutionProgramConfigViewModel CreateExecutionProgramConfigViewModel(IExecutionProgramObjectModel model) {
			return null!;
		}

		public IExecutionConfigView CreateExecutionConfigView(IExecutionProgramConfigViewModel viewModel) {
			return null!;
		}
	}

	private sealed class TestMediaItemTypeProvider : IMediaItemTypeProvider {
		public TestMediaItemTypeProvider(MediaType mediaType, string createdBy) {
			this.MediaType = mediaType;
			this.CreatedBy = createdBy;
		}

		public MediaType MediaType {
			get;
		}

		public string CreatedBy {
			get;
		}

		public IQueryable<MediaItem> IncludeTables(IQueryable<MediaItem> MediaItems) {
			return MediaItems.Concat(new[] { CreateMediaItem($@"C:\included\{this.CreatedBy}.dat") }).AsQueryable();
		}

		public bool IsTargetPath(string path) {
			return this.MediaType switch {
				MediaType.Image => Path.GetExtension(path).Equals(".jpg", StringComparison.OrdinalIgnoreCase),
				MediaType.Video => Path.GetExtension(path).Equals(".mp4", StringComparison.OrdinalIgnoreCase),
				_ => false
			};
		}

		public MediaItemPathStatus GetPathStatus(string path) {
			return new(true, 0, DateTime.MinValue, DateTime.MinValue, DateTime.MinValue);
		}

		public Task ExecuteAsync(string filePath, IServiceProvider scopedServiceProvider) {
			return Task.CompletedTask;
		}
	}

	private sealed class TestFileModel : ModelBase, IMediaItemModel {
		private readonly Subject<Unit> _changed = new();

		public TestFileModel(MediaType mediaType, string filePath, string createdBy) {
			this.MediaType = mediaType;
			this.FilePath = filePath;
			this.CreatedBy = createdBy;
		}

		public Observable<Unit> Changed {
			get {
				return this._changed.AsObservable();
			}
		}

		public MediaType MediaType {
			get;
		}

		public long Id {
			get;
		}

		public string FilePath {
			get;
		}

		public string CreatedBy {
			get;
		}

		public string? ThumbnailFilePath {
			get;
			set;
		}

		public bool Exists {
			get;
			set;
		} = true;

		public IGpsLocation? Location {
			get;
			set;
		}

		public List<ITagModel> Tags {
			get;
			set;
		} = [];

		public ComparableSize? Resolution {
			get;
			set;
		}

		public int Rate {
			get;
			set;
		}

		public int UsageCount {
			get;
			set;
		}

		public string Description {
			get;
			set;
		} = string.Empty;

		public DateTime CreationTime {
			get;
			set;
		}

		public DateTime ModifiedTime {
			get;
			set;
		}

		public DateTime LastAccessTime {
			get;
			set;
		}

		public DateTime RegisteredTime {
			get;
			set;
		}

		public long FileSize {
			get;
			set;
		}

		public Attributes<string> Properties {
			get;
		} = new();

		public Task UpdateRateAsync(int rate) {
			this.Rate = rate;
			return Task.CompletedTask;
		}

		public Task IncrementUsageCountAsync() {
			this.UsageCount++;
			return Task.CompletedTask;
		}

		public Task UpdateDescriptionAsync(string description) {
			this.Description = description;
			return Task.CompletedTask;
		}

		public Task ExecuteFileAsync() {
			return Task.CompletedTask;
		}

	}

	private sealed class TestFileViewModel : ViewModelBase, IMediaItemViewModel {
		public TestFileViewModel(IMediaItemModel fileModel, string createdBy) {
			this.FileModel = fileModel;
			this.FilePath = fileModel.FilePath;
			this.ThumbnailFilePath = new BindableReactiveProperty<string>(fileModel.ThumbnailFilePath ?? string.Empty);
			this.Exists = fileModel.Exists;
			this.Properties = fileModel.Properties;
			this.MediaType = fileModel.MediaType;
			this.Location = fileModel.Location;
			this.CreatedBy = createdBy;
		}

		public IMediaItemModel FileModel {
			get;
		}

		public string FilePath {
			get;
		}

		public BindableReactiveProperty<string> ThumbnailFilePath {
			get;
		}

		public IThumbnailControlView ThumbnailControlView {
			get {
				return new TestThumbnailControlView(this.CreatedBy) { DataContext = this };
			}
		}

		public bool Exists {
			get;
		}

		public Attributes<string> Properties {
			get;
		}

		public MediaType MediaType {
			get;
		}

		public IGpsLocation? Location {
			get;
		}

		public string CreatedBy {
			get;
		}

		public Task ExecuteFileAsync() {
			return Task.CompletedTask;
		}

		public void RefreshThumbnail() {
		}
	}

	private sealed class TestDetailViewerPreviewControlView : IDetailViewerPreviewControlView {
		public TestDetailViewerPreviewControlView(string createdBy) {
			this.CreatedBy = createdBy;
		}

		public object DataContext {
			get;
			set;
		} = null!;

		public string CreatedBy {
			get;
		}
	}

	private sealed class TestThumbnailControlView : IThumbnailControlView {
		public TestThumbnailControlView(string createdBy) {
			this.CreatedBy = createdBy;
		}

		public object DataContext {
			get;
			set;
		} = null!;

		public string CreatedBy {
			get;
		}
	}

	private sealed class TestThumbnailPickerViewModel : ViewModelBase, IThumbnailPickerViewModel {
		public TestThumbnailPickerViewModel(string createdBy) {
			this.CreatedBy = createdBy;
		}

		public BindableReactiveProperty<byte[]?> OriginalThumbnail {
			get;
		} = new((byte[]?)null);

		public BindableReactiveProperty<byte[]?> CandidateThumbnail {
			get;
		} = new((byte[]?)null);

		public ReactiveCommand RecreateThumbnailCommand {
			get;
		} = new();

		public ReactiveCommand PickThumbnailFromFileCommand {
			get;
		} = new();

		public ReactiveCommand SaveCommand {
			get;
		} = new();

		public string CreatedBy {
			get;
		}

		public void RecreateThumbnail() {
		}

		public Task SaveAsync() {
			return Task.CompletedTask;
		}

		public Task LoadAsync(IMediaItemViewModel fileViewModel) {
			return Task.CompletedTask;
		}
	}

	private sealed class TestThumbnailPickerView : IThumbnailPickerView {
		public TestThumbnailPickerView(string createdBy) {
			this.CreatedBy = createdBy;
		}

		public object DataContext {
			get;
			set;
		} = null!;

		public string CreatedBy {
			get;
		}
	}

	private sealed class TestFileOperator : IMediaItemOperator {
		public TestFileOperator(MediaType targetMediaType, string createdBy) {
			this.TargetMediaType = targetMediaType;
			this.CreatedBy = createdBy;
		}

		public MediaType TargetMediaType {
			get;
		}

		public string CreatedBy {
			get;
		}

		public Task<MediaItem?> RegisterMediaItemAsync(string filePath) {
			return Task.FromResult<MediaItem?>(null);
		}

		public Task<MediaItem?> UpdateRateAsync(long MediaItemId, int rate) {
			return Task.FromResult<MediaItem?>(null);
		}

		public Task<MediaItem?> IncrementUsageCountAsync(long MediaItemId) {
			return Task.FromResult<MediaItem?>(null);
		}

		public Task<MediaItem?> UpdateDescriptionAsync(long MediaItemId, string description) {
			return Task.FromResult<MediaItem?>(null);
		}
	}
}