using MediaDeck.Database.Tables;
using System.Linq.Expressions;

namespace MediaDeck.Models.Repositories.Objects; 
public abstract class RepositoryConditionObject {
	/// <summary>
	/// 読み込み条件絞り込み
	/// </summary>
	/// <returns>絞り込み関数</returns>
	public abstract Expression<Func<MediaFile, bool>> WherePredicate();
}
