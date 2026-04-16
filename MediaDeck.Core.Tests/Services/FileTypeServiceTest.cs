using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.FileTypes;
using MediaDeck.Composition.Interfaces.FileTypes.Models;
using MediaDeck.Composition.Interfaces.FileTypes.ViewModels;
using MediaDeck.Composition.Interfaces.FileTypes.Views;
using MediaDeck.Composition.Interfaces.Primitives;
using MediaDeck.Composition.Interfaces.Services;
using MediaDeck.Composition.Interfaces.Tags;
using MediaDeck.Composition.Objects;
using MediaDeck.Core.Services;
using MediaDeck.Database.Tables;

using R3;

using Shouldly;

namespace MediaDeck.Core.Tests.Services;

/// <summary>
/// <see cref="FileTypeService" /> のテストクラス。
/// </summary>
public class FileTypeServiceTest {
	/// <summary>
	/// Unknown用のテストファイルタイプ。
	/// </summary>
	private static readonly TestFileType UnknownFileType = new(MediaType.Unknown, "unknown");

	/// <summary>
	/// Image用のテストファイルタイプ。
	/// </summary>
	private static readonly TestFileType ImageFileType = new(MediaType.Image, "image");

	/// <summary>
	/// Video用のテストファイルタイプ。
	/// </summary>
	private static readonly TestFileType VideoFileType = new(MediaType.Video, "video");

	private readonly FileTypeService _service;

	public FileTypeServiceTest() {
		var fileTypes = new[] { UnknownFileType, ImageFileType, VideoFileType };
		this._service = new FileTypeService(fileTypes, new TestFilePathService());
	}

	/// <summary>
	/// 画像ファイルのレコードから画像用モデルが生成されることを確認する。
	/// </summary>
	[Fact]
	public void CreateFileModelFromRecord_UsesMatchingFileType() {
		var mediaFile = CreateMediaFile(@"C:\media\sample.jpg");

		var result = this._service.CreateFileModelFromRecord(mediaFile);

		var testFileModel = result.ShouldBeOfType<TestFileModel>();
		testFileModel.CreatedBy.ShouldBe("image");
	}

	/// <summary>
	/// 未登録の拡張子はUnknown用モデルにフォールバックすることを確認する。
	/// </summary>
	[Fact]
	public void CreateFileModelFromRecord_FallsBackToUnknownWhenMediaTypeIsNotRegistered() {
		var mediaFile = CreateMediaFile(@"C:\media\sample.txt");

		var result = this._service.CreateFileModelFromRecord(mediaFile);

		var testFileModel = result.ShouldBeOfType<TestFileModel>();
		testFileModel.CreatedBy.ShouldBe("unknown");
	}

	/// <summary>
	/// ファイルモデルのメディアタイプに対応するビューモデルが生成されることを確認する。
	/// </summary>
	[Fact]
	public void CreateFileViewModel_UsesMatchingFileType() {
		var fileModel = new TestFileModel(MediaType.Video, @"C:\media\sample.mp4", "input");

		var result = this._service.CreateFileViewModel(fileModel);

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
	public void ThumbnailFactories_UseMatchingFileType() {
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
	public void CreateFileOperators_ReturnsOperatorsForAllRegisteredFileTypes() {
		var result = this._service.CreateFileOperators();

		result.Count().ShouldBe(3);
		result.ShouldAllBe(x => x is TestFileOperator);
		result.Cast<TestFileOperator>().Select(x => x.CreatedBy).ToArray().ShouldBe(["unknown", "image", "video"]);
	}

	/// <summary>
	/// IncludeTablesが登録済みのすべてのファイルタイプに順番に委譲されることを確認する。
	/// </summary>
	[Fact]
	public void IncludeTables_AppliesAllRegisteredFileTypes() {
		var mediaFiles = new[] { CreateMediaFile(@"C:\media\base.dat") }.AsQueryable();

		var result = this._service.IncludeTables(mediaFiles).ToList();

		result.Select(x => x.FilePath).ToArray()
			.ShouldBe([@"C:\media\base.dat",
				@"C:\included\unknown.dat",
				@"C:\included\image.dat",
				@"C:\included\video.dat"]);
	}

	/// <summary>
	/// テスト用の <see cref="MediaFile" /> を生成する。
	/// </summary>
	/// <param name="filePath">ファイルパス。</param>
	/// <returns>生成した <see cref="MediaFile" />。</returns>
	private static MediaFile CreateMediaFile(string filePath) {
		return new MediaFile { DirectoryPath = Path.GetDirectoryName(filePath) ?? string.Empty, FilePath = filePath, Description = string.Empty, MediaFileTags = new List<MediaFileTag>() };
	}

	/// <summary>
	/// テスト用のファイルパスサービス。
	/// </summary>
	private sealed class TestFilePathService : IFilePathService {
		public string GetThumbnailRelativeFilePath() {
			return string.Empty;
		}

		public string GetThumbnailAbsoluteFilePath(string thumbRelativePath) {
			return thumbRelativePath;
		}

		public bool IsTargetFile(string path) {
			return this.GetMediaType(path) is not null;
		}

		public bool IsVideoFile(string path) {
			return this.GetMediaType(path) == MediaType.Video;
		}

		public bool IsImageFile(string path) {
			return this.GetMediaType(path) == MediaType.Image;
		}

		public MediaType? GetMediaType(string path) {
			return Path.GetExtension(path).ToLowerInvariant() switch {
				".jpg" => MediaType.Image,
				".mp4" => MediaType.Video,
				_ => null
			};
		}
	}

	private sealed class TestFileType : IFileType {
		public TestFileType(MediaType mediaType, string createdBy) {
			this.MediaType = mediaType;
			this.CreatedBy = createdBy;
		}

		public MediaType MediaType {
			get;
		}

		public string CreatedBy {
			get;
		}

		public IFileOperator CreateFileOperator() {
			return new TestFileOperator(this.MediaType, this.CreatedBy);
		}

		public IFileModel CreateFileModelFromRecord(MediaFile mediaFile) {
			return new TestFileModel(this.MediaType, mediaFile.FilePath, this.CreatedBy);
		}

		public IDetailViewerPreviewControlView CreateDetailViewerPreviewControlView(IFileViewModel fileViewModel) {
			return new TestDetailViewerPreviewControlView(this.CreatedBy) { DataContext = fileViewModel };
		}

		public IFileViewModel CreateFileViewModel(IFileModel fileModel) {
			return new TestFileViewModel(fileModel, this.CreatedBy);
		}

		public IThumbnailPickerViewModel CreateThumbnailPickerViewModel() {
			return new TestThumbnailPickerViewModel(this.CreatedBy);
		}

		public IThumbnailPickerView CreateThumbnailPickerView() {
			return new TestThumbnailPickerView(this.CreatedBy);
		}

		public IQueryable<MediaFile> IncludeTables(IQueryable<MediaFile> mediaFiles) {
			return mediaFiles.Concat(new[] { CreateMediaFile($@"C:\included\{this.CreatedBy}.dat") }).AsQueryable();
		}
	}

	private sealed class TestFileModel : IFileModel {
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

	private sealed class TestFileViewModel : IFileViewModel {
		public TestFileViewModel(IFileModel fileModel, string createdBy) {
			this.FileModel = fileModel;
			this.FilePath = fileModel.FilePath;
			this.ThumbnailFilePath = new BindableReactiveProperty<string>(fileModel.ThumbnailFilePath ?? string.Empty);
			this.Exists = fileModel.Exists;
			this.Properties = fileModel.Properties;
			this.MediaType = fileModel.MediaType;
			this.Location = fileModel.Location;
			this.CreatedBy = createdBy;
		}

		public IFileModel FileModel {
			get;
		}

		public string FilePath {
			get;
		}

		public BindableReactiveProperty<string> ThumbnailFilePath {
			get;
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

	private sealed class TestThumbnailPickerViewModel : IThumbnailPickerViewModel {
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

		public Task LoadAsync(IFileViewModel fileViewModel) {
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

	private sealed class TestFileOperator : IFileOperator {
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

		public Task<MediaFile?> RegisterFileAsync(string filePath) {
			return Task.FromResult<MediaFile?>(null);
		}

		public Task<MediaFile?> UpdateRateAsync(long mediaFileId, int rate) {
			return Task.FromResult<MediaFile?>(null);
		}

		public Task<MediaFile?> IncrementUsageCountAsync(long mediaFileId) {
			return Task.FromResult<MediaFile?>(null);
		}

		public Task<MediaFile?> UpdateDescriptionAsync(long mediaFileId, string description) {
			return Task.FromResult<MediaFile?>(null);
		}
	}
}