using MediaDeck.Database;
using MediaDeck.Database.Tables;

namespace MediaDeck;

public static class InitialDataRegisterer
{
	public static void Register(MediaDeckDbContext db) {
		using var transaction = db.Database.BeginTransaction();
		if(db.TagCategories.Count() != 0){
			return;
		}
		db.TagCategories.AddRange([
			new TagCategory() { TagCategoryId = -1, TagCategoryName = "No Category", Tags = [], Detail = "No Category Tags" }
		]);
		transaction.Commit();
		db.SaveChanges();
	}
}
