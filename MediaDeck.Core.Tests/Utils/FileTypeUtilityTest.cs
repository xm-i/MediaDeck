using System.IO;

using CommunityToolkit.Mvvm.DependencyInjection;

using Shouldly;

using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.Files;
using MediaDeck.Composition.Interfaces.FileTypes;
using MediaDeck.Composition.Interfaces.FileTypes.Models;
using MediaDeck.Composition.Interfaces.FileTypes.ViewModels;
using MediaDeck.Composition.Interfaces.FileTypes.Views;
using MediaDeck.Composition.Interfaces.Primitives;
using MediaDeck.Composition.Interfaces.Services;
using MediaDeck.Composition.Objects;
using MediaDeck.Core.Utils;
using MediaDeck.Database.Tables;
using MediaDeck.Database.Tables.Metadata;

using Microsoft.Extensions.DependencyInjection;

using R3;

namespace MediaDeck.Core.Tests.Utils;

/// <summary>
/// <see cref="FileTypeUtility" /> のテストクラス。
/// </summary>
public class FileTypeUtilityTest {
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

	/// <summary>
	/// 静的コンストラクタ。
	/// </summary>
	static FileTypeUtilityTest() {
		var services = new ServiceCollection();
		services.AddSingleton<IFilePathService, TestFilePathService>();
		services.AddSingleton<IFileType>(UnknownFileType);
		services.AddSingleton<IFileType>(ImageFileType);
		services.AddSingleton<IFileType>(VideoFileType);
		Ioc.Default.ConfigureServices(services.BuildServiceProvider());
	}

	/// <summary>
	/// 画像ファイルのレコードから画像用モデルが生成されることを確認する。
	/// </summary>
	[Fact]
	public void CreateFileModelFromRecord_UsesMatchingFileType() {
		var mediaFile = CreateMediaFile(@"C:\media\sample.jpg");

		var result = FileTypeUtility.CreateFileModelFromRecord(mediaFile);

		var testFileModel = result.ShouldBeOfType<TestFileModel>();
		testFileModel.CreatedBy.ShouldBe("image");
	}

	/// <summary>
	/// 未登録の拡張子はUnknown用モデルにフォールバックすることを確認する。
	/// </summary>
	[Fact]
	public void CreateFileModelFromRecord_FallsBackToUnknownWhenMediaTypeIsNotRegistered() {
		var mediaFile = CreateMediaFile(@"C:\media\sample.txt");

		var result = FileTypeUtility.CreateFileModelFromRecord(mediaFile);

		var testFileModel = result.ShouldBeOfType<TestFileModel>();
		testFileModel.CreatedBy.ShouldBe("unknown");
	}

	/// <summary>
	/// ファイルモデルのメディアタイプに対応するビューモデルが生成されることを確認する。
	/// </summary>
	[Fact]
	public void CreateFileViewModel_UsesMatchingFileType() {
		var fileModel = new TestFileModel(MediaType.Video, @"C:\media\sample.mp4", "input");

		var result = FileTypeUtility.CreateFileViewModel(fileModel);

		var testFileViewModel = result.ShouldBeOfType<TestFileViewModel>();
		testFileViewModel.CreatedBy.ShouldBe("video");
	}

	/// <summary>
	/// 未登録のメディアタイプを持つビューモデルではUnknown用プレビューが生成されることを確認する。
	/// </summary>
	[Fact]
	public void CreateDetailViewerPreviewControlView_FallsBackToUnknownWhenMediaTypeIsNotRegistered() {
		var fileViewModel = new TestFileViewModel(new TestFileModel(MediaType.Pdf, @"C:\media\sample.pdf", "input"), "input");

		var result = FileTypeUtility.CreateDetailViewerPreviewControlView(fileViewModel);

		var testView = result.ShouldBeOfType<TestDetailViewerPreviewControlView>();
		testView.CreatedBy.ShouldBe("unknown");
	}

	/// <summary>
	/// サムネイル関連のファクトリが対応するファイルタイプを使用することを確認する。
	/// </summary>
	[Fact]
	public void ThumbnailFactories_UseMatchingFileType() {
		var fileViewModel = new TestFileViewModel(new TestFileModel(MediaType.Video, @"C:\media\sample.mp4", "input"), "input");

		var thumbnailPickerViewModel = FileTypeUtility.CreateThumbnailPickerViewModel(fileViewModel);
		var thumbnailPickerView = FileTypeUtility.CreateThumbnailPickerView(fileViewModel);

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
		var result = FileTypeUtility.CreateFileOperators();

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

		var result = FileTypeUtility.IncludeTables(mediaFiles).ToList();

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
		/// <summary>
		/// サムネイル相対パスを取得する。
		/// </summary>
		/// <returns>空文字列。</returns>
		public string GetThumbnailRelativeFilePath() {
			return string.Empty;
		}

		/// <summary>
		/// サムネイル絶対パスを取得する。
		/// </summary>
		/// <param name="thumbRelativePath">サムネイル相対パス。</param>
		/// <returns>入力値。</returns>
		public string GetThumbnailAbsoluteFilePath(string thumbRelativePath) {
			return thumbRelativePath;
		}

		/// <summary>
		/// 管理対象ファイルかどうかを判定する。
		/// </summary>
		/// <param name="path">ファイルパス。</param>
		/// <returns>管理対象の場合は<c>true</c>。</returns>
		public bool IsTargetFile(string path) {
			return this.GetMediaType(path) is not null;
		}

		/// <summary>
		/// 動画ファイルかどうかを判定する。
		/// </summary>
		/// <param name="path">ファイルパス。</param>
		/// <returns>動画ファイルの場合は<c>true</c>。</returns>
		public bool IsVideoFile(string path) {
			return this.GetMediaType(path) == MediaType.Video;
		}

		/// <summary>
		/// 画像ファイルかどうかを判定する。
		/// </summary>
		/// <param name="path">ファイルパス。</param>
		/// <returns>画像ファイルの場合は<c>true</c>。</returns>
		public bool IsImageFile(string path) {
			return this.GetMediaType(path) == MediaType.Image;
		}

		/// <summary>
		/// ファイルパスからメディアタイプを取得する。
		/// </summary>
		/// <param name="path">ファイルパス。</param>
		/// <returns>判定できたメディアタイプ。</returns>
		public MediaType? GetMediaType(string path) {
			return Path.GetExtension(path).ToLowerInvariant() switch {
				".jpg" => MediaType.Image,
				".mp4" => MediaType.Video,
				_ => null
			};
		}
	}

	/// <summary>
	/// テスト用のファイルタイプ。
	/// </summary>
	private sealed class TestFileType : IFileType {
		/// <summary>
		/// コンストラクタ。
		/// </summary>
		/// <param name="mediaType">担当するメディアタイプ。</param>
		/// <param name="createdBy">生成元を識別する文字列。</param>
		public TestFileType(MediaType mediaType, string createdBy) {
			this.MediaType = mediaType;
			this.CreatedBy = createdBy;
		}

		/// <summary>
		/// 担当するメディアタイプ。
		/// </summary>
		public MediaType MediaType {
			get;
		}

		/// <summary>
		/// 生成元を識別する文字列。
		/// </summary>
		public string CreatedBy {
			get;
		}

		/// <summary>
		/// ファイルオペレーターを生成する。
		/// </summary>
		/// <returns>生成したファイルオペレーター。</returns>
		public IFileOperator CreateFileOperator() {
			return new TestFileOperator(this.MediaType, this.CreatedBy);
		}

		/// <summary>
		/// レコードからファイルモデルを生成する。
		/// </summary>
		/// <param name="mediaFile">メディアファイルレコード。</param>
		/// <returns>生成したファイルモデル。</returns>
		public IFileModel CreateFileModelFromRecord(MediaFile mediaFile) {
			return new TestFileModel(this.MediaType, mediaFile.FilePath, this.CreatedBy);
		}

		/// <summary>
		/// ビューモデルからプレビューコントロールを生成する。
		/// </summary>
		/// <param name="fileViewModel">対象のファイルビューモデル。</param>
		/// <returns>生成したプレビューコントロール。</returns>
		public IDetailViewerPreviewControlView CreateDetailViewerPreviewControlView(IFileViewModel fileViewModel) {
			return new TestDetailViewerPreviewControlView(this.CreatedBy) { DataContext = fileViewModel };
		}

		/// <summary>
		/// ファイルモデルからビューモデルを生成する。
		/// </summary>
		/// <param name="fileModel">対象のファイルモデル。</param>
		/// <returns>生成したファイルビューモデル。</returns>
		public IFileViewModel CreateFileViewModel(IFileModel fileModel) {
			return new TestFileViewModel(fileModel, this.CreatedBy);
		}

		/// <summary>
		/// サムネイルピッカービューモデルを生成する。
		/// </summary>
		/// <returns>生成したサムネイルピッカービューモデル。</returns>
		public IThumbnailPickerViewModel CreateThumbnailPickerViewModel() {
			return new TestThumbnailPickerViewModel(this.CreatedBy);
		}

		/// <summary>
		/// サムネイルピッカービューを生成する。
		/// </summary>
		/// <returns>生成したサムネイルピッカービュー。</returns>
		public IThumbnailPickerView CreateThumbnailPickerView() {
			return new TestThumbnailPickerView(this.CreatedBy);
		}

		/// <summary>
		/// IncludeTablesの呼び出し結果にテスト用レコードを追加する。
		/// </summary>
		/// <param name="mediaFiles">入力クエリ。</param>
		/// <returns>テスト用レコードを追加したクエリ。</returns>
		public IQueryable<MediaFile> IncludeTables(IQueryable<MediaFile> mediaFiles) {
			return mediaFiles.Concat(new[] { CreateMediaFile($@"C:\included\{this.CreatedBy}.dat") }).AsQueryable();
		}
	}

	/// <summary>
	/// テスト用のファイルモデル。
	/// </summary>
	private sealed class TestFileModel : IFileModel {
		/// <summary>
		/// 変更通知用Subject。
		/// </summary>
		private readonly Subject<Unit> _changed = new();

		/// <summary>
		/// コンストラクタ。
		/// </summary>
		/// <param name="mediaType">メディアタイプ。</param>
		/// <param name="filePath">ファイルパス。</param>
		/// <param name="createdBy">生成元を識別する文字列。</param>
		public TestFileModel(MediaType mediaType, string filePath, string createdBy) {
			this.MediaType = mediaType;
			this.FilePath = filePath;
			this.CreatedBy = createdBy;
		}

		/// <summary>
		/// 変更通知。
		/// </summary>
		public Observable<Unit> Changed {
			get {
				return this._changed.AsObservable();
			}
		}

		/// <summary>
		/// メディアタイプ。
		/// </summary>
		public MediaType MediaType {
			get;
		}

		/// <summary>
		/// ID。
		/// </summary>
		public long Id {
			get;
		}

		/// <summary>
		/// ファイルパス。
		/// </summary>
		public string FilePath {
			get;
		}

		/// <summary>
		/// 生成元を識別する文字列。
		/// </summary>
		public string CreatedBy {
			get;
		}

		/// <summary>
		/// サムネイルファイルパス。
		/// </summary>
		public string? ThumbnailFilePath {
			get;
			set;
		}

		/// <summary>
		/// ファイルが存在するかどうか。
		/// </summary>
		public bool Exists {
			get;
			set;
		} = true;

		/// <summary>
		/// 位置情報。
		/// </summary>
		public IGpsLocation? Location {
			get;
			set;
		}

		/// <summary>
		/// タグ一覧。
		/// </summary>
		public List<ITagModel> Tags {
			get;
			set;
		} = [];

		/// <summary>
		/// 解像度。
		/// </summary>
		public ComparableSize? Resolution {
			get;
			set;
		}

		/// <summary>
		/// 評価。
		/// </summary>
		public int Rate {
			get;
			set;
		}

		/// <summary>
		/// 使用回数。
		/// </summary>
		public int UsageCount {
			get;
			set;
		}

		/// <summary>
		/// 説明。
		/// </summary>
		public string Description {
			get;
			set;
		} = string.Empty;

		/// <summary>
		/// 作成日時。
		/// </summary>
		public DateTime CreationTime {
			get;
			set;
		}

		/// <summary>
		/// 更新日時。
		/// </summary>
		public DateTime ModifiedTime {
			get;
			set;
		}

		/// <summary>
		/// 最終アクセス日時。
		/// </summary>
		public DateTime LastAccessTime {
			get;
			set;
		}

		/// <summary>
		/// 登録日時。
		/// </summary>
		public DateTime RegisteredTime {
			get;
			set;
		}

		/// <summary>
		/// ファイルサイズ。
		/// </summary>
		public long FileSize {
			get;
			set;
		}

		/// <summary>
		/// 属性一覧。
		/// </summary>
		public Attributes<string> Properties {
			get;
		} = new();

		/// <summary>
		/// 評価を更新する。
		/// </summary>
		/// <param name="rate">更新後の評価。</param>
		/// <returns>完了タスク。</returns>
		public Task UpdateRateAsync(int rate) {
			this.Rate = rate;
			return Task.CompletedTask;
		}

		/// <summary>
		/// 使用回数を増やす。
		/// </summary>
		/// <returns>完了タスク。</returns>
		public Task IncrementUsageCountAsync() {
			this.UsageCount++;
			return Task.CompletedTask;
		}

		/// <summary>
		/// 説明を更新する。
		/// </summary>
		/// <param name="description">更新後の説明。</param>
		/// <returns>完了タスク。</returns>
		public Task UpdateDescriptionAsync(string description) {
			this.Description = description;
			return Task.CompletedTask;
		}

		/// <summary>
		/// ファイルを実行する。
		/// </summary>
		/// <returns>完了タスク。</returns>
		public Task ExecuteFileAsync() {
			return Task.CompletedTask;
		}
	}

	/// <summary>
	/// テスト用のファイルビューモデル。
	/// </summary>
	private sealed class TestFileViewModel : IFileViewModel {
		/// <summary>
		/// コンストラクタ。
		/// </summary>
		/// <param name="fileModel">元になるファイルモデル。</param>
		/// <param name="createdBy">生成元を識別する文字列。</param>
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

		/// <summary>
		/// 元のファイルモデル。
		/// </summary>
		public IFileModel FileModel {
			get;
		}

		/// <summary>
		/// ファイルパス。
		/// </summary>
		public string FilePath {
			get;
		}

		/// <summary>
		/// サムネイルファイルパス。
		/// </summary>
		public BindableReactiveProperty<string> ThumbnailFilePath {
			get;
		}

		/// <summary>
		/// ファイルが存在するかどうか。
		/// </summary>
		public bool Exists {
			get;
		}

		/// <summary>
		/// 属性一覧。
		/// </summary>
		public Attributes<string> Properties {
			get;
		}

		/// <summary>
		/// メディアタイプ。
		/// </summary>
		public MediaType MediaType {
			get;
		}

		/// <summary>
		/// 位置情報。
		/// </summary>
		public IGpsLocation? Location {
			get;
		}

		/// <summary>
		/// 生成元を識別する文字列。
		/// </summary>
		public string CreatedBy {
			get;
		}

		/// <summary>
		/// ファイルを実行する。
		/// </summary>
		/// <returns>完了タスク。</returns>
		public Task ExecuteFileAsync() {
			return Task.CompletedTask;
		}

		/// <summary>
		/// サムネイルを更新する。
		/// </summary>
		public void RefreshThumbnail() { }
	}

	/// <summary>
	/// テスト用のプレビューコントロール。
	/// </summary>
	private sealed class TestDetailViewerPreviewControlView : IDetailViewerPreviewControlView {
		/// <summary>
		/// コンストラクタ。
		/// </summary>
		/// <param name="createdBy">生成元を識別する文字列。</param>
		public TestDetailViewerPreviewControlView(string createdBy) {
			this.CreatedBy = createdBy;
		}

		/// <summary>
		/// DataContext。
		/// </summary>
		public object DataContext {
			get;
			set;
		} = null!;

		/// <summary>
		/// 生成元を識別する文字列。
		/// </summary>
		public string CreatedBy {
			get;
		}
	}

	/// <summary>
	/// テスト用のサムネイルピッカービューモデル。
	/// </summary>
	private sealed class TestThumbnailPickerViewModel : IThumbnailPickerViewModel {
		/// <summary>
		/// コンストラクタ。
		/// </summary>
		/// <param name="createdBy">生成元を識別する文字列。</param>
		public TestThumbnailPickerViewModel(string createdBy) {
			this.CreatedBy = createdBy;
		}

		/// <summary>
		/// 元画像。
		/// </summary>
		public BindableReactiveProperty<byte[]?> OriginalThumbnail {
			get;
		} = new((byte[]?)null);

		/// <summary>
		/// 候補画像。
		/// </summary>
		public BindableReactiveProperty<byte[]?> CandidateThumbnail {
			get;
		} = new((byte[]?)null);

		/// <summary>
		/// 再生成コマンド。
		/// </summary>
		public ReactiveCommand RecreateThumbnailCommand {
			get;
		} = new();

		/// <summary>
		/// 保存コマンド。
		/// </summary>
		public ReactiveCommand SaveCommand {
			get;
		} = new();

		/// <summary>
		/// 生成元を識別する文字列。
		/// </summary>
		public string CreatedBy {
			get;
		}

		/// <summary>
		/// サムネイルを再生成する。
		/// </summary>
		public void RecreateThumbnail() { }

		/// <summary>
		/// サムネイルを保存する。
		/// </summary>
		/// <returns>完了タスク。</returns>
		public Task SaveAsync() {
			return Task.CompletedTask;
		}

		/// <summary>
		/// 対象のファイルビューモデルを読み込む。
		/// </summary>
		/// <param name="fileViewModel">対象のファイルビューモデル。</param>
		/// <returns>完了タスク。</returns>
		public Task LoadAsync(IFileViewModel fileViewModel) {
			return Task.CompletedTask;
		}
	}

	/// <summary>
	/// テスト用のサムネイルピッカービュー。
	/// </summary>
	private sealed class TestThumbnailPickerView : IThumbnailPickerView {
		/// <summary>
		/// コンストラクタ。
		/// </summary>
		/// <param name="createdBy">生成元を識別する文字列。</param>
		public TestThumbnailPickerView(string createdBy) {
			this.CreatedBy = createdBy;
		}

		/// <summary>
		/// DataContext。
		/// </summary>
		public object DataContext {
			get;
			set;
		} = null!;

		/// <summary>
		/// 生成元を識別する文字列。
		/// </summary>
		public string CreatedBy {
			get;
		}
	}

	/// <summary>
	/// テスト用のファイルオペレーター。
	/// </summary>
	private sealed class TestFileOperator : IFileOperator {
		/// <summary>
		/// コンストラクタ。
		/// </summary>
		/// <param name="targetMediaType">対象メディアタイプ。</param>
		/// <param name="createdBy">生成元を識別する文字列。</param>
		public TestFileOperator(MediaType targetMediaType, string createdBy) {
			this.TargetMediaType = targetMediaType;
			this.CreatedBy = createdBy;
		}

		/// <summary>
		/// 対象メディアタイプ。
		/// </summary>
		public MediaType TargetMediaType {
			get;
		}

		/// <summary>
		/// 生成元を識別する文字列。
		/// </summary>
		public string CreatedBy {
			get;
		}

		/// <summary>
		/// ファイル登録を行う。
		/// </summary>
		/// <param name="filePath">登録対象ファイルパス。</param>
		/// <returns>常に<c>null</c>。</returns>
		public Task<MediaFile?> RegisterFileAsync(string filePath) {
			return Task.FromResult<MediaFile?>(null);
		}

		/// <summary>
		/// 評価更新を行う。
		/// </summary>
		/// <param name="mediaFileId">メディアファイルID。</param>
		/// <param name="rate">評価。</param>
		/// <returns>常に<c>null</c>。</returns>
		public Task<MediaFile?> UpdateRateAsync(long mediaFileId, int rate) {
			return Task.FromResult<MediaFile?>(null);
		}

		/// <summary>
		/// 使用回数を更新する。
		/// </summary>
		/// <param name="mediaFileId">メディアファイルID。</param>
		/// <returns>常に<c>null</c>。</returns>
		public Task<MediaFile?> IncrementUsageCountAsync(long mediaFileId) {
			return Task.FromResult<MediaFile?>(null);
		}

		/// <summary>
		/// 説明を更新する。
		/// </summary>
		/// <param name="mediaFileId">メディアファイルID。</param>
		/// <param name="description">説明。</param>
		/// <returns>常に<c>null</c>。</returns>
		public Task<MediaFile?> UpdateDescriptionAsync(long mediaFileId, string description) {
			return Task.FromResult<MediaFile?>(null);
		}
	}
}