using System.IO;
using System.Linq.Expressions;

using MediaDeck.Database.Tables;

namespace MediaDeck.Models.Repositories.Objects; 
public class FolderRepositoryConditionObject : RepositoryConditionObject {
	public required string DirectoryPath {
		get;
		init;
	}

	public required bool IncludeSubDirectories {
		get;
		init;
	}

	public override Expression<Func<MediaFile, bool>> WherePredicate() {
		if (this.DirectoryPath == null) {
			throw new InvalidOperationException();
		}
		if (this.IncludeSubDirectories) {
			return mediaFile =>
				mediaFile.DirectoryPath == this.DirectoryPath || mediaFile.DirectoryPath.StartsWith($"{this.DirectoryPath}{Path.DirectorySeparatorChar}");
		} else {
			return mediaFile =>
				mediaFile.DirectoryPath == this.DirectoryPath;
		}
	}
}
