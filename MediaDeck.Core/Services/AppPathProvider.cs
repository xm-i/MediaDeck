using MediaDeck.Composition.Constants;

namespace MediaDeck.Core.Services;

[Inject(InjectServiceLifetime.Singleton, typeof(IAppPathProvider))]
public class AppPathProvider : IAppPathProvider {
	public string BaseDirectory {
		get {
			return FilePathConstants.BaseDirectory;
		}
	}

	public string StateFilePath {
		get {
			return FilePathConstants.StateFilePath;
		}
	}

	public string ConfigFilePath {
		get {
			return FilePathConstants.ConfigFilePath;
		}
	}

	public string NoThumbnailFilePath {
		get {
			return FilePathConstants.NoThumbnailFilePath;
		}
	}
}