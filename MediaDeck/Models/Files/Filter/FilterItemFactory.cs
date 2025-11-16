using System.IO;

using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.Files;
using MediaDeck.Models.Files.Filter.FilterItemObjects;

namespace MediaDeck.Models.Files.Filter;
public static class FilterItemFactory {

	public static FilterItem Create<T>(T filterItemObject) where T : IFilterItemObject {
		switch (filterItemObject) {
			case ExistsFilterItemObject ef:
				return new FilterItem(
					x => File.Exists(x.FilePath) == ef.Exists,
					x => x.Exists == ef.Exists,
					false);
			case FilePathFilterItemObject fpf:
				return new FilterItem(
					x => x.FilePath.Contains(fpf.Text) == (fpf.SearchType == SearchTypeInclude.Include),
					x => x.FilePath.Contains(fpf.Text) == (fpf.SearchType == SearchTypeInclude.Include),
					true);
			case LocationFilterItemObject lf:
				if (lf.Text != null) {
					return new FilterItem(
						x => x.Position!.DisplayName!.Contains(lf.Text),
						// TODO : とりあえず現状では素通ししておく。モデル側にもロケーション名の情報を読み込む必要がある。ロケーション名は後から生成されることもあるので、生成されたときにモデル側にも反映する必要もあり。結構大掛かりになりそうなのであとまわし
						x => true,
						true);
				}
				if (lf.Contains is { } b) {
					return new FilterItem(
						x => (x.Latitude == null && x.Longitude == null) != b,
						x => x.Location == null != b,
						true);
				}
				if (lf.LeftTop != null && lf.RightBottom != null) {
					return new FilterItem(x =>
							lf.LeftTop.Latitude > x.Latitude &&
						x.Latitude > lf.RightBottom.Latitude &&
						lf.LeftTop.Longitude < x.Longitude &&
						lf.RightBottom.Longitude > x.Longitude,
						x => lf.LeftTop > x.Location && x.Location > lf.RightBottom,
						true);
				}
				return new FilterItem(x => false, x => false, true);
			case MediaTypeFilterItemObject mtf:
				return new FilterItem(
					x => x.FilePath.IsVideoFile() == mtf.IsVideo,
					x => x.FilePath.IsVideoFile() == mtf.IsVideo,
					false);
			case RateFilterItemObject rf:
				var op = SearchTypeConverters.SearchTypeToFunc<int>(rf.SearchType);
				return new FilterItem(
					x => op(x.Rate, rf.Rate),
					x => op(x.Rate, rf.Rate),
					false);
			case ResolutionFilterItemObject resolutionFilter:
				var rop = SearchTypeConverters.SearchTypeToFunc<double?>(resolutionFilter.SearchType);
				if (resolutionFilter.Width is { } w) {
					return new FilterItem(
						x => rop(x.Width, w),
						x => rop(x.Resolution?.Width, w),
						false);
				} else if (resolutionFilter.Height is { } h) {
					return new FilterItem(
						x => rop(x.Height, h),
						x => rop(x.Resolution?.Height, h),
						false);
				} else if (resolutionFilter.Resolution is { } r) {
					return new FilterItem(
						x => rop(x.Width * x.Height, r.Area),
						x => rop(x.Resolution?.Area, r.Area),
						false);
				}
				throw new InvalidOperationException();
			case TagFilterItemObject tf:
				return new FilterItem(
					x => x.MediaFileTags.Select(mft => mft.Tag.TagName).Contains(tf.TagName) == (tf.SearchType == SearchTypeInclude.Include),
					x => x.Tags.Any(x => x.TagName == tf.TagName) == (tf.SearchType == SearchTypeInclude.Include),
					false);
			default:
				throw new ArgumentException("undefined filter", nameof(filterItemObject));
		}

	}
}
