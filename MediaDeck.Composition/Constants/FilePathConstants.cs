using System.IO;

namespace MediaDeck.Composition.Constants;
public static class FilePathConstants {
	public static string BaseDirectory {
		get;
	} = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MediaDeck");
	public static string NoThumbnailFilePath {
		get;
	} = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"Assets", "thumbnail_creation_failed.png");

	public static string StateFilePath {
		get;
	} = Path.Combine(BaseDirectory, "MediaDeck.states");

	public static string ConfigFilePath {
		get;
	} = Path.Combine(BaseDirectory, "MediaDeck.config");
}
