using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Tables;

namespace MediaDeck.Composition.Interfaces.MediaItemTypes.Models;

public interface IMediaItemOperator {
	public MediaType TargetMediaType {
		get;
	}

	public Task<MediaItem?> RegisterMediaItemAsync(string filePath);

	public Task<MediaItem?> UpdateRateAsync(long MediaItemId, int rate);

	public Task<MediaItem?> IncrementUsageCountAsync(long MediaItemId);

	public Task<MediaItem?> UpdateDescriptionAsync(long MediaItemId, string description);
}