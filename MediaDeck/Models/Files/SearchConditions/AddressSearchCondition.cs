using System.Linq.Expressions;

using MediaDeck.Database.Tables;
using MediaDeck.FileTypes.Base.Models.Interfaces;
using MediaDeck.Models.Maps;

namespace MediaDeck.Models.Files.SearchConditions;
public class AddressSearchCondition: ISearchCondition {
	[Obsolete("for serialize")]
	public AddressSearchCondition() {
		this.Address = null!;
	}
	public AddressSearchCondition(Address address) {
		this.Address = address;
	}

	public Address Address {
		get;
		set;
	}

	public bool IncludeSubDirectories {
		get;
		set;
	}

	public string DisplayText {
		get {
			return $"Address={this.Address.Name}";
		}
	}

	public Expression<Func<MediaFile, bool>>? WherePredicate {
		get {
			Expression<Func<MediaFile, bool>> exp1 = mediaFile => true;
			var exp = exp1.Body;
			var visitor = new ParameterVisitor(exp1.Parameters);

			if (!this.Address.IsFailure && !this.Address.IsYet) {
				var current = this.Address;
				while (current is { } c && c.Type != null) {
					Expression<Func<MediaFile, bool>> exp2 = mediaFile =>
					mediaFile.Position!.Addresses!.Any(a => a.Type == c.Type && a.Name == c.Name);
					exp = Expression.AndAlso(exp, visitor.Visit(exp2.Body));
					current = current.Parent;
				}
			} else {
				Expression<Func<MediaFile, bool>> exp2 = mediaFile =>
					mediaFile.Latitude != null && mediaFile.Position!.IsAcquired != this.Address.IsYet && mediaFile.Position.Addresses!.IsEmpty();
				exp = Expression.AndAlso(exp, visitor.Visit(exp2.Body)
					);
			}
			return Expression.Lambda<Func<MediaFile, bool>>(
				exp,
				visitor.Parameters);
		}
	}

	public Func<IFileModel, bool>? Filter {
		get;
	} = null;

	public bool IsMatchForSuggest(string searchWord) {
		return this.Address.Name?.Contains(searchWord) ?? false;
	}
}
