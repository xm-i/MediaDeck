using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.Files;
using MediaDeck.Composition.Interfaces.FileTypes.Models;
using MediaDeck.Composition.Interfaces.Primitives;
using MediaDeck.Composition.Objects;
using R3;

namespace MediaDeck.Core.Tests.Models.Files.Sort;

/// <summary>
/// <see cref="IFileModel"/> のテスト用スタブ実装
/// </summary>
public class TestFileModel : IFileModel {
	public Observable<Unit> Changed { get; } = Observable.Empty<Unit>();
	public MediaType MediaType {
		get; set;
	}
	public long Id {
		get; set;
	}
	public string FilePath { get; set; } = string.Empty;
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
	public int Rate {
		get; set;
	}
	public int UsageCount {
		get; set;
	}
	public string Description { get; set; } = string.Empty;
	public DateTime CreationTime {
		get; set;
	}
	public DateTime ModifiedTime {
		get; set;
	}
	public DateTime LastAccessTime {
		get; set;
	}
	public DateTime RegisteredTime {
		get; set;
	}
	public long FileSize {
		get; set;
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
}