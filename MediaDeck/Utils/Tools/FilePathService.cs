using System.Text;
using System.Security.Cryptography;
using System.IO;
using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.Services;
using MediaDeck.Composition.Stores.Config.Model;

namespace MediaDeck.Utils.Tools;

[Inject(InjectServiceLifetime.Singleton, typeof(IFilePathService))]
public class FilePathService : IFilePathService {
	private readonly ConfigModel _config;

	public FilePathService(ConfigModel config) {
		this._config = config;
	}

	/// <inheritdoc/>
	public string GetThumbnailRelativeFilePath(string filePath) {
		return $"{string.Join("", SHA512.HashData(Encoding.UTF8.GetBytes(filePath)).Select(b => $"{b:X2}")).Insert(2, @"\")}.jpg";
	}

	/// <inheritdoc/>
	public string GetThumbnailAbsoluteFilePath(string thumbRelativePath) {
		return Path.Combine(this._config.PathConfig.ThumbnailFolderPath.Value, thumbRelativePath);
	}

	/// <inheritdoc/>
	public bool IsTargetFile(string path) {
		return this._config.ScanConfig.TargetExtensions.Any(x => x.Extension.Value.Equals(Path.GetExtension(path), StringComparison.CurrentCultureIgnoreCase));
	}

	/// <inheritdoc/>
	public bool IsVideoFile(string path) {
		return this._config.ScanConfig.TargetExtensions.Where(x => x.MediaType.Value == MediaType.Video).Any(x => x.Extension.Value.Equals(Path.GetExtension(path), StringComparison.CurrentCultureIgnoreCase));
	}

	/// <inheritdoc/>
	public bool IsImageFile(string path) {
		return this._config.ScanConfig.TargetExtensions.Where(x => x.MediaType.Value == MediaType.Image).Any(x => x.Extension.Value.Equals(Path.GetExtension(path), StringComparison.CurrentCultureIgnoreCase));
	}

	/// <inheritdoc/>
	public MediaType? GetMediaType(string path) {
		return this._config.ScanConfig.TargetExtensions.Where(x => x.Extension.Value.Equals(Path.GetExtension(path), StringComparison.CurrentCultureIgnoreCase)).Select(x => x.MediaType.Value as MediaType?).FirstOrDefault();
	}
}
