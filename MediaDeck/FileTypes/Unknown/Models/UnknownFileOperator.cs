using System.Threading.Tasks;

using MediaDeck.Composition.Enum;
using MediaDeck.Database.Tables;
using MediaDeck.FileTypes.Base.Models;

namespace MediaDeck.FileTypes.Unknown.Models;
[Inject(InjectServiceLifetime.Transient)]
public partial class UnknownFileOperator : BaseFileOperator {

	public override MediaType TargetMediaType {
		get;
	} = MediaType.Unknown;

	public override Task<MediaFile?> RegisterFileAsync(string filePath) {
		throw new NotSupportedException("Unknown file type cannot be registered.");
	}
}
