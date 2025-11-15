using System.Threading.Tasks;

using MediaDeck.Composition.Enum;
using MediaDeck.Database.Tables;

namespace MediaDeck.FileTypes.Base.Models.Interfaces;
public interface IFileOperator {
	public MediaType TargetMediaType {
		get;
	}

	public Task<MediaFile?> RegisterFileAsync(string filePath);

	public Task<MediaFile?> UpdateRateAsync(long mediaFileId, int rate);

	public Task<MediaFile?> IncrementUsageCountAsync(long mediaFileId);

	public Task<MediaFile?> UpdateDescriptionAsync(long mediaFileId, string description);
}
