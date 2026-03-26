using MediaDeck.Database.Tables;

namespace MediaDeck.Composition.Interfaces.Files;

public interface ITagModelFactory {
	public ITagModel Create(Tag tag);
	public ITagCategoryModel Create(TagCategory tagCategory);
	public ITagAliasModel Create(TagAlias tagAlias);
}