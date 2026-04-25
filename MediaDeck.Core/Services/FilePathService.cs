using MediaDeck.Composition.Stores.Config.Model;

namespace MediaDeck.Core.Services;

[Inject(InjectServiceLifetime.Singleton, typeof(IFilePathService))]
public class FilePathService : IFilePathService {
	private readonly ConfigModel _config;

	public FilePathService(ConfigModel config) {
		this._config = config;
	}

	/// <inheritdoc/>
	public string GetThumbnailRelativeFilePath() {
		var uuid = Guid.NewGuid().ToString("N");
		return @$"{uuid[..2]}\{uuid[2..]}.jpg";
	}

	/// <inheritdoc/>
	public string GetThumbnailAbsoluteFilePath(string thumbRelativePath) {
		return Path.Combine(this._config.PathConfig.ThumbnailFolderPath.Value, thumbRelativePath);
	}

}