using CommunityToolkit.Mvvm.DependencyInjection;

using MediaDeck.Models.Files.Filter.FilterItemCreators;
using MediaDeck.Models.Files.Filter.FilterItemObjects;

namespace MediaDeck.Models.Files.Filter;
public static class FilterItemFactory {

	public static FilterItem Create<T>(T filterItemObject) where T : IFilterItemObject {
		switch (filterItemObject) {
			case ExistsFilterItemObject ef:
				var efIc = Ioc.Default.GetRequiredService<ExistsFilterItemCreator>();
				return efIc.Create(ef);
			case FilePathFilterItemObject fpf:
				var fpfIc = Ioc.Default.GetRequiredService<FilePathFilterItemCreator>();
				return fpfIc.Create(fpf);
			case LocationFilterItemObject lf:
				var lfIc = Ioc.Default.GetRequiredService<LocationFilterItemCreator>();
				return lfIc.Create(lf);
			case MediaTypeFilterItemObject mtf:
				var mtfIc = Ioc.Default.GetRequiredService<MediaTypeFilterItemCreator>();
				return mtfIc.Create(mtf);
			case RateFilterItemObject rf:
				var rfIc = Ioc.Default.GetRequiredService<RateFilterItemCreator>();
				return rfIc.Create(rf);
			case ResolutionFilterItemObject resolutionFilter:
				var resolutionFilterIc = Ioc.Default.GetRequiredService<ResolutionFilterItemCreator>();
				return resolutionFilterIc.Create(resolutionFilter);
			case TagFilterItemObject tf:
				var tfIc = Ioc.Default.GetRequiredService<TagFilterItemCreator>();
				return tfIc.Create(tf);
			default:
				throw new ArgumentException("undefined filter", nameof(filterItemObject));
		}

	}
}
