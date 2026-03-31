using System.Threading.Tasks;

using MediaDeck.Composition.Enum;
using MediaDeck.Database.Tables;
using MediaDeck.FileTypes.Base.Models;

namespace MediaDeck.FileTypes.Unknown.Models;

[Inject(InjectServiceLifetime.Transient)]
internal partial class UnknownFileOperator : BaseFileOperator {
	public UnknownFileOperator() : base(MediaType.Unknown) { }

	public override Task<MediaFile?> RegisterFileAsync(string filePath) {
		throw new NotSupportedException("Unknown file type cannot be registered.");
	}
}