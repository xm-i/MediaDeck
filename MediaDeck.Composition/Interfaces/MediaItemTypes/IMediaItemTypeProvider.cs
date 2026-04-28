using MediaDeck.Composition.Enum;
using MediaDeck.Database.Tables;

namespace MediaDeck.Composition.Interfaces.MediaItemTypes;

public interface IMediaItemTypeProvider {
	public MediaType MediaType {
		get;
	}

	public IQueryable<MediaItem> IncludeTables(IQueryable<MediaItem> MediaItems);
	public bool IsTargetPath(string path);
	public MediaItemPathStatus GetPathStatus(string path);

	/// <summary>
	/// 指定されたファイルパスに対してこのメディアタイプ固有の実行処理を行う。
	/// </summary>
	/// <param name="filePath">実行対象のファイルパス</param>
	/// <param name="scopedServiceProvider">実行するタブのスコープを切ったサービスプロバイダー</param>
	/// <returns>非同期タスク</returns>
	public Task ExecuteAsync(string filePath, IServiceProvider scopedServiceProvider);
}

public readonly struct MediaItemPathStatus {
	public MediaItemPathStatus(bool exists, long fileSize, DateTime creationTime, DateTime modifiedTime, DateTime lastAccessTime) {
		this.Exists = exists;
		this.FileSize = fileSize;
		this.CreationTime = creationTime;
		this.ModifiedTime = modifiedTime;
		this.LastAccessTime = lastAccessTime;
	}

	public bool Exists {
		get;
	}

	public long FileSize {
		get;
	}

	public DateTime CreationTime {
		get;
	}

	public DateTime ModifiedTime {
		get;
	}

	public DateTime LastAccessTime {
		get;
	}
}