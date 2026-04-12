using System.Linq.Expressions;

using MediaDeck.Composition.Interfaces.Files;
using MediaDeck.Composition.Interfaces.FileTypes.Models;
using MediaDeck.Database.Tables;

using R3.JsonConfig.Attributes;

namespace MediaDeck.Core.Models.Files.SearchConditions;

[GenerateR3JsonConfigDto]
[JsonConfigDerivedType("folder")]
[Inject(InjectServiceLifetime.Transient)]
public class FolderSearchCondition : ISearchCondition {
	public FolderSearchCondition() {
	}

	public string FolderPath {
		get {
			return field ?? throw new InvalidOperationException($"{nameof(this.FolderPath)} is not initialized.");
		}
		set {
			field = value;
		}
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