using System.IO;

namespace MediaDeck.Utils.Constants;
public static class FilePathConstants {
	public static string BaseDirectory {
		get;
	} = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MediaDeck");
	public static string NoThumbnailFilePath {
		get;
	} = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"Assets", "thumbnail_creation_failed.png");
}
