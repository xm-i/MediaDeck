using MediaDeck.Database;
using MediaDeck.Database.Tables;

namespace MediaDeck;

public static class InitialDataRegisterer {
	public static void Register(MediaDeckDbContext db) {
		using var transaction = db.Database.BeginTransaction();
		if (db.TagCategories.Any()) {
			return;
		}
		db.TagCategories.AddRange([
			new() { TagCategoryId = -1, TagCategoryName = "No Category", Tags = [], Detail = "No Category Tags" }
		]);
		transaction.Commit();
		db.SaveChanges();
	}
}