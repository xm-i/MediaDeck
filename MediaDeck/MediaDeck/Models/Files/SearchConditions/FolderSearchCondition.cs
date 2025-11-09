using System.IO;
using System.Linq.Expressions;

using MediaDeck.Database.Tables;
using MediaDeck.FileTypes.Base.Models.Interfaces;
using MediaDeck.Models.Repositories.Objects;

namespace MediaDeck.Models.Files.SearchConditions;
public class FolderSearchCondition: ISearchCondition {
	[Obsolete("for serialize")]
	public FolderSearchCondition() {
		this.FolderPath = null!;
	}
	public FolderSearchCondition(FolderObject folderObject) {
		this.FolderPath = folderObject.FolderPath;
	}

	public string FolderPath{
		get;
		set;
	}

	public bool IncludeSubDirectories {
		get;
		set;
	}

	public string DisplayText {
		get {
			return $"Folder={this.FolderPath}{(this.IncludeSubDirectories ? "&IncludeSubDirectories" : "")}";
		}
	}

	public Expression<Func<MediaFile, bool>>? WherePredicate {
		get {
			if (this.IncludeSubDirectories) {
				return mediaFile =>
					mediaFile.DirectoryPath == this.FolderPath || mediaFile.DirectoryPath.StartsWith($"{this.FolderPath}{Path.DirectorySeparatorChar}");
			} else {
				return mediaFile =>
					mediaFile.DirectoryPath == this.FolderPath;
			}
		}
	}
	public Func<IFileModel, bool>? Filter {
		get;
	} = null;

	public bool IsMatchForSuggest(string searchWord) {
		return this.FolderPath.Contains(searchWord, StringComparison.CurrentCultureIgnoreCase);
	}
}
