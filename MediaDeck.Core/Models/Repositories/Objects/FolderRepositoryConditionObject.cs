using System.Linq.Expressions;

using MediaDeck.Database.Tables;

namespace MediaDeck.Core.Models.Repositories.Objects;

public class FolderRepositoryConditionObject : RepositoryConditionObject {
	public required string DirectoryPath {
		get;
		init;
	}

	public required bool IncludeSubDirectories {
		get;
		init;
	}

	public override Expression<Func<MediaItem, bool>> WherePredicate() {
		if (this.DirectoryPath == null) {
			throw new InvalidOperationException();
		}
		if (this.IncludeSubDirectories) {
			return MediaItem =>
				MediaItem.DirectoryPath == this.DirectoryPath || MediaItem.DirectoryPath.StartsWith($"{this.DirectoryPath}{Path.DirectorySeparatorChar}");
		} else {
			return MediaItem =>
				MediaItem.DirectoryPath == this.DirectoryPath;
		}
	}
}