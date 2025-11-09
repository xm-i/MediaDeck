using System.IO;
using Windows.Storage;

namespace MediaDeck.Utils.Constants;
public static class FilePathConstants {
	public static string BaseDirectory {
		get;
	} = ApplicationData.Current.LocalFolder.Path;
	public static string NoThumbnailFilePath {
		get;
	} = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"Assets", "thumbnail_creation_failed.png");
}
