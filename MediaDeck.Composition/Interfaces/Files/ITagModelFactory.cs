using MediaDeck.Database.Tables;

namespace MediaDeck.Composition.Interfaces.Files;

public interface ITagModelFactory {
	public ITagModel Create(Tag tag);
	public ITagModel Create(Tag tag, ITagCategoryModel category);
	public ITagCategoryModel Create(TagCategory tagCategory);
	public ITagAliasModel Create(TagAlias tagAlias);
	public ITagCategoryModel CreateCategory();
	public ITagAliasModel CreateAlias();
}