using System.Linq.Expressions;

using MediaDeck.Common.Utilities;
using MediaDeck.Composition.Interfaces.Files;
using MediaDeck.Core.Models.Maps;
using MediaDeck.Database.Tables;

using R3.JsonConfig.Attributes;

namespace MediaDeck.Core.Models.Files.SearchConditions;

[GenerateR3JsonConfigDto]
[JsonConfigDerivedType("address")]
[Inject(InjectServiceLifetime.Transient)]
public class AddressSearchCondition : ISearchCondition {
	public AddressSearchCondition() {
	}

	public Address Address {
		get {
			return field ?? throw new InvalidOperationException($"{nameof(this.Address)} is not initialized.");
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
					mediaFile.Latitude != null && mediaFile.Position!.IsAcquired != this.Address.IsYet && !mediaFile.Position.Addresses!.Any();
				exp = Expression.AndAlso(exp, visitor.Visit(exp2.Body));
			}
			return Expression.Lambda<Func<MediaFile, bool>>(exp,
				visitor.Parameters);
		}
	}

	public bool IsMatchForSuggest(string searchWord) {
		return this.Address.Name?.Contains(searchWord) ?? false;
	}
}