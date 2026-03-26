using MediaDeck.Composition.Interfaces.Files;
using MediaDeck.Database.Tables;

namespace MediaDeck.Models.Files.Tags;


[Inject(InjectServiceLifetime.Singleton, typeof(ITagModelFactory))]
public class TagModelFactory : ITagModelFactory {
	public ITagModel Create(Tag tag) {
		return new TagModel(tag);
	}

	public ITagCategoryModel Create(TagCategory tagCategory) {
		return new TagCategoryModel(tagCategory);
	}

	public ITagAliasModel Create(TagAlias tagAlias) {
		return new TagAliasModel(tagAlias);
	}
}
