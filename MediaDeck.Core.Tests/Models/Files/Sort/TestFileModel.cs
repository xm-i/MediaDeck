using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.MediaItemTypes.Models;
using MediaDeck.Composition.Interfaces.Primitives;
using MediaDeck.Composition.Interfaces.Tags;
using MediaDeck.Composition.Objects;
using MediaDeck.Database.Tables;
using R3;

namespace MediaDeck.Core.Tests.Models.Files.Sort;

/// <summary>
/// <see cref="IMediaItemModel"/> のテスト用スタブ実装
/// </summary>
public class TestFileModel : MediaItem, IMediaItemModel {
	[global::System.Diagnostics.CodeAnalysis.SetsRequiredMembers]
	public TestFileModel() {
		this.DirectoryPath = string.Empty;
		this.FilePath = string.Empty;
		this.Description = string.Empty;
	}

	public Observable<Unit> Changed { get; } = Observable.Empty<Unit>();
	public MediaType MediaType {
		get; set;
	}
	public long Id {
		get {
			return this.MediaItemId;
		}

		set {
			this.MediaItemId = value;
		}
	}
	public string? ThumbnailFilePath {
		get; set;
	}
	public bool Exists {
		get; set;
	}
	public IGpsLocation? Location {
		get; set;
	}
	public List<ITagModel> Tags { get; set; } = new();
	public ComparableSize? Resolution {
		get; set;
	}
	public new int Rate {
		get {
			return base.Rate;
		}

		set {
			base.Rate = value;
		}
	}
	public new int UsageCount {
		get {
			return base.UsageCount;
		}

		set {
			base.UsageCount = value;
		}
	}
	public new DateTime CreationTime {
		get {
			return base.CreationTime;
		}

		set {
			base.CreationTime = value;
		}
	}
	public new DateTime ModifiedTime {
		get {
			return base.ModifiedTime;
		}

		set {
			base.ModifiedTime = value;
		}
	}
	public new DateTime LastAccessTime {
		get {
			return base.LastAccessTime;
		}

		set {
			base.LastAccessTime = value;
		}
	}
	public new DateTime RegisteredTime {
		get {
			return base.RegisteredTime;
		}

		set {
			base.RegisteredTime = value;
		}
	}
	public new long FileSize {
		get {
			return base.FileSize;
		}

		set {
			base.FileSize = value;
		}
	}
	public Attributes<string> Properties { get; } = new();

	public Task UpdateRateAsync(int rate) {
		return Task.CompletedTask;
	}

	public Task IncrementUsageCountAsync() {
		return Task.CompletedTask;
	}

	public Task UpdateDescriptionAsync(string description) {
		return Task.CompletedTask;
	}

	public Task ExecuteFileAsync() {
		return Task.CompletedTask;
	}

	/// <summary>テスト用スタブのため何もしない。</summary>
	public void Dispose() {
	}
}