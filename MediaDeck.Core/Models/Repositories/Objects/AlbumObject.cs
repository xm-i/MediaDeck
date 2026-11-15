using MediaDeck.Core.Primitives;

namespace MediaDeck.Core.Models.Repositories.Objects;

/// <summary>
/// アルバム階層ノード。仮想 Path を '/' で分割して階層を表現する。
/// </summary>
public partial class AlbumObject {
	public const char Separator = '/';

	public string AlbumPath {
		get;
		set;
	}

	public AlbumObject? Parent {
		get;
		set;
	}

	public AlbumObject[] ChildAlbums {
		get;
		set;
	}

	public string AlbumName {
		get;
		set;
	}

	public long FileCount {
		get;
		set;
	}

	public bool IsExpanded {
		get;
		set;
	} = false;

	/// <summary>
	/// このアルバムを削除可能かどうか（子アルバムを持たない単アルバムかどうか）を取得します。
	/// </summary>
	public bool CanDelete {
		get {
			return this.ChildAlbums.Length == 0;
		}
	}

	public AlbumObject(AlbumObject? parent, string currentPath, ValueCountPair<string>[] albumPaths) {
		this.Parent = parent;
		this.AlbumPath = currentPath;

		var children = new List<AlbumObject>();
		foreach (var album in albumPaths) {
			if (album.Value == this.AlbumPath) {
				continue;
			}
			if (children.Any(x => album.Value.StartsWith($"{x.AlbumPath}{Separator}"))) {
				continue;
			}
			children.Add(new(this, album.Value,
				albumPaths.Where(x => $"{x.Value}{Separator}".StartsWith($"{album.Value}{Separator}")).ToArray()));
		}
		this.ChildAlbums = [.. children];

		if (parent == null) {
			this.AlbumName = "Albums";
		} else if (string.IsNullOrWhiteSpace(parent.AlbumPath)) {
			this.AlbumName = this.AlbumPath;
		} else {
			this.AlbumName = this.AlbumPath.Replace($"{parent.AlbumPath}{Separator}", "");
		}
		this.FileCount = albumPaths.Sum(x => x.Count);
	}
}