using System.Linq.Expressions;
using MediaDeck.Composition.Interfaces.Notifications;
using MediaDeck.Composition.Tables;
using R3.JsonConfig.Attributes;

namespace MediaDeck.Core.Models.Files.SearchConditions;

[GenerateR3JsonConfigDto]
[JsonConfigDerivedType("folder")]
[Inject(InjectServiceLifetime.Transient)]
[Inject(InjectServiceLifetime.Transient, typeof(IFolderSearchCondition))]
public class FolderSearchCondition : IFolderSearchCondition {
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

	public Expression<Func<MediaItem, bool>>? WherePredicate {
		get {
			if (this.IncludeSubDirectories) {
				return MediaItem =>
					MediaItem.DirectoryPath == this.FolderPath || MediaItem.DirectoryPath.StartsWith($"{this.FolderPath}{Path.DirectorySeparatorChar}");
			} else {
				return MediaItem =>
					MediaItem.DirectoryPath == this.FolderPath;
			}
		}
	}

	public bool IsMatchForSuggest(string searchWord) {
		return this.FolderPath.Contains(searchWord, StringComparison.CurrentCultureIgnoreCase);
	}
}