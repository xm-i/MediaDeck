using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.Files;
using MediaDeck.Core.Models.Files.Filter.FilterItemObjects;
using MediaDeck.Database.Tables;

namespace MediaDeck.Core.Models.Files.Filter;

public static class FilterItemFactory {
	public static FilterItem Create<T>(T filterItemObject) where T : IFilterItemObject {
		switch (filterItemObject) {
			case ExistsFilterItemObject ef:
				return new FilterItem(x => x.IsExists == ef.Exists);
			case FilePathFilterItemObject fpf:
				return new FilterItem(x => x.FilePath.Contains(fpf.Text) == (fpf.SearchType == SearchTypeInclude.Include));
			case LocationFilterItemObject lf:
				if (lf.Text != null) {
					return new FilterItem(x => x.Position!.DisplayName!.Contains(lf.Text));
				}
				if (lf.Contains is { } b) {
					return new FilterItem(x => (x.Latitude == null && x.Longitude == null) != b);
				}
				if (lf.LeftTop != null && lf.RightBottom != null) {
					return new FilterItem(x =>
						lf.LeftTop.Latitude > x.Latitude &&
						x.Latitude > lf.RightBottom.Latitude &&
						lf.LeftTop.Longitude < x.Longitude &&
						lf.RightBottom.Longitude > x.Longitude);
				}
				return new FilterItem(x => false);
			case MediaTypeFilterItemObject mtf:
				return new FilterItem(x => (x.VideoFile != null) == mtf.IsVideo);
			case RateFilterItemObject rf:
				return new FilterItem(SearchTypeConverters.BuildComparisonExpression<MediaItem, int>(rf.SearchType, x => x.Rate, rf.Rate));
			case ResolutionFilterItemObject resolutionFilter:
				if (resolutionFilter.Width is { } w) {
					return new FilterItem(SearchTypeConverters.BuildComparisonExpression<MediaItem, int>(resolutionFilter.SearchType, x => x.Width, w));
				} else if (resolutionFilter.Height is { } h) {
					return new FilterItem(SearchTypeConverters.BuildComparisonExpression<MediaItem, int>(resolutionFilter.SearchType, x => x.Height, h));
				} else if (resolutionFilter.Resolution is { } r) {
					return new FilterItem(SearchTypeConverters.BuildComparisonExpression<MediaItem, long>(resolutionFilter.SearchType, x => (long)x.Width * x.Height, (long)r.Area));
				}
				throw new InvalidOperationException();
			case TagFilterItemObject tf:
				return new FilterItem(x => x.MediaItemTags.Select(mft => mft.Tag.TagName).Contains(tf.TagName) == (tf.SearchType == SearchTypeInclude.Include));
			default:
				throw new ArgumentException("undefined filter", nameof(filterItemObject));
		}
	}
}