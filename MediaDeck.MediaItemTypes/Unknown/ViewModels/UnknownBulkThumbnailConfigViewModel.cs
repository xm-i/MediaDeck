using System.Threading;
using System.Threading.Tasks;

using MediaDeck.Common.Base;
using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.MediaItemTypes.ViewModels;

namespace MediaDeck.MediaItemTypes.Unknown.ViewModels;

/// <summary>
/// Unknown 用のダミー設定 ViewModel。何もしない。
/// </summary>
[Inject(InjectServiceLifetime.Transient)]
public class UnknownBulkThumbnailConfigViewModel : ViewModelBase, IBulkThumbnailConfigViewModel {
	public MediaType MediaType {
		get;
	} = MediaType.Unknown;

	public Task ApplyToAsync(IMediaItemViewModel target, CancellationToken cancellationToken) {
		return Task.CompletedTask;
	}
}