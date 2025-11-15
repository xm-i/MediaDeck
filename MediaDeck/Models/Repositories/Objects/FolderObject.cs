using System.Collections.Generic;
using System.IO;

using MediaDeck.Utils.Objects;

namespace MediaDeck.Models.Repositories.Objects;

public partial class FolderObject {
	public string FolderPath {
		get;
		set;
	}

	public FolderObject? Parent {
		get;
		set;
	}

	public FolderObject[] ChildFolders {
		get;
		set;
	}

	public string FolderName {
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

	[Obsolete("for serialize")]
	public FolderObject() {
		this.Parent = null!;
		this.ChildFolders = null!;
		this.FolderPath = null!;
		this.FolderName = null!;
	}

	public FolderObject(FolderObject? parent, string currentPath, ValueCountPair<string>[] directories) {
		this.Parent = parent;
		this.FolderPath = currentPath;

		// 子フォルダを設定
		var children = new List<FolderObject>();
		foreach (var dir in directories) {
			if (dir.Value == this.FolderPath) {
				// 自分自身は除外
				continue;
			}
			if (children.Any(x => dir.Value.StartsWith($"{x.FolderPath}{Path.DirectorySeparatorChar}"))) {
				// すでに親が含まれている
				continue;
			}
			children.Add(new(this, dir.Value, directories.Where(x => $"{x.Value}{Path.DirectorySeparatorChar}".StartsWith($"{dir.Value}{Path.DirectorySeparatorChar}")).ToArray()));
		}
		this.ChildFolders = [.. children];

		if (parent == null) {
			this.FolderName = "PC";
		} else {
			if (string.IsNullOrWhiteSpace(parent.FolderPath)) {
				this.FolderName = this.FolderPath;
			} else {
				this.FolderName = this.FolderPath.Replace($"{parent.FolderPath}{Path.DirectorySeparatorChar}", "");
			}
		}
		this.FileCount = directories.Sum(x => x.Count);
	}
}
